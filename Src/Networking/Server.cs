using System;
using System.IO;
using Riptide;
using System.Collections.Generic;

//engine stuff
using static SpatialEngine.Globals;
using static SpatialEngine.Rendering.MeshUtils;
using Riptide.Transports;
using System.Collections;
using SpatialEngine.Networking.Packets;
using SpatialGame;

namespace SpatialEngine.Networking
{
    //client and server
    //updates get sent here and sent out here
    //Client Server arch:
    /*
        client sends input updates to server and server will apply that update
        server can also be host
        host computer will auto update its self and send that update to all clients
        
        every frame:
        check for stuff from clients (input)
        if there is input
            run the update based on that input (enum with switch?)
        run host update
        send all update to client (position of things, rotation of things)
    
    */
    
    public class SpatialServer
    {
        public static Server server;

        public ushort port;
        public string ip;
        public int maxConnections { get; protected set; }
        //first is the current id and the key is the client id from the server which riptide does not auto correct
        public Dictionary<uint, uint> connectionIds;
        uint connectionCount = 0;

        bool stopping;


        public SpatialServer(string ip, ushort port = 58301, int maxConnections = 10)
        {
            this.ip = ip;
            this.port = port;
            this.maxConnections = maxConnections;
            Message.InstancesPerPeer = 100;
            connectionIds = new Dictionary<uint, uint>();
        }

        public void Start()
        {
            server = new Server();
            server.MessageReceived += handleMessage;
            server.ClientConnected += ClientConnected;
            server.ClientDisconnected += ClientDisconnected;
            server.Start(port, (ushort)maxConnections, 0, false);
        }

        public void Stop()
        {
            stopping = true;
            server.Stop();
            NetworkManager.isServer = false;
            NetworkManager.running = false;
        }

        public void Update(float deltaTime)
        {
            if(!stopping)
            {
                server.Update();
            }
        }

        public void ClientConnected(object sender, ServerConnectedEventArgs e)
        {
            connectionIds.Add(e.Client.Id, (uint)connectionIds.Count);
            foreach (var item in connectionIds)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
        }

        public void ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            //reset all values in connectionId after the left client as we need to bring down it by one
            for (uint i = connectionIds[e.Client.Id]; i < server.Clients.Length; i++)
            {
                //and skip the first client as will be -1
                //if (connectionIds[server.Clients[i].Id] != 0)
                //{
                    //set the values at the keys of each client after the left client to be one less
                    connectionIds[server.Clients[i].Id] = connectionIds[server.Clients[i].Id] - 1;
                //}
            }

            connectionCount--;

            PlayerLeavePacket packet = new PlayerLeavePacket();

            //using the algorithm for sending the player packets
            int currentId = (int)connectionIds[e.Client.Id];

            //start from one as we cannot access the client list as the client has been removed
            for (int i = 0; i < connectionCount; i++)
            {
                //if we are the same client we skip
                if (i == e.Client.Id)
                    continue;

                if (currentId > connectionIds[server.Clients[i].Id])
                {
                    packet.clientId = currentId - 1;
                    SendRelib(packet, server.Clients[i].Id);
                }
                else
                {
                    packet.clientId = currentId;
                    SendRelib(packet, server.Clients[i].Id);
                }
            }
            connectionIds.Remove(e.Client.Id);

        }

        public Connection[] GetServerConnections()
        {
            if(stopping)
            {
                return [];
            }
            return server.Clients;
        }

        public void SendUnrelib(Packet packet, ushort clientId)
        {
            if(!stopping)
            {
                Message msgUnrelib = Message.Create(MessageSendMode.Unreliable, packet.GetPacketType());
                msgUnrelib.AddBytes(packet.ConvertToByte());
                server.Send(msgUnrelib, clientId);
            }
        }

        public void SendRelib(Packet packet, ushort clientId)
        {
            if (!stopping)
            {
                Message msgRelib = Message.Create(MessageSendMode.Reliable, packet.GetPacketType());
                msgRelib.AddBytes(packet.ConvertToByte());
                server.Send(msgRelib, clientId);
            }
        }

        public void SendUnrelibAll(Packet packet)
        {
            if (!stopping)
            {
                Message msgUnrelib = Message.Create(MessageSendMode.Unreliable, packet.GetPacketType());
                msgUnrelib.AddBytes(packet.ConvertToByte());
                server.SendToAll(msgUnrelib);
            }
        }

        public void SendRelibAll(Packet packet)
        {
            if (!stopping)
            {
                Message msgRelib = Message.Create(MessageSendMode.Reliable, packet.GetPacketType());
                msgRelib.AddBytes(packet.ConvertToByte());
                server.SendToAll(msgRelib);
            }
        }

        public void SendUnrelibAllExclude(Packet packet, ushort clientId)
        {
            if (!stopping)
            {
                Message msgUnrelib = Message.Create(MessageSendMode.Unreliable, packet.GetPacketType());
                msgUnrelib.AddBytes(packet.ConvertToByte());
                for (int i = 0; i < server.Clients.Length; i++)
                {
                    if(clientId != server.Clients[i].Id)
                        server.Send(msgUnrelib, server.Clients[i]);
                }
            }
        }

        public void SendRelibAllExclude(Packet packet, ushort clientId)
        {
            if (!stopping)
            {
                Message msgRelib = Message.Create(MessageSendMode.Reliable, packet.GetPacketType());
                msgRelib.AddBytes(packet.ConvertToByte());
                for (int i = 0; i < server.Clients.Length; i++)
                {
                    if (clientId != server.Clients[i].Id)
                    {
                        server.Send(msgRelib, server.Clients[i]);
                    }
                }
            }
        }

        public void handleMessage(object sender, MessageReceivedEventArgs e)
        {
            if (!stopping)
                HandlePacketServer(e.Message.GetBytes(), e.FromConnection);
        }

        public void Close()
        {
            stopping = true;
            server.Stop();
            server = null;
        }

        //Handles packets that come from the client

        void HandlePacketServer(byte[] data, Connection client)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            //data sent is not a proper packet
            if (data.Length < 2)
                return;

            //packet type
            ushort type = reader.ReadUInt16();

            switch (type)
            {
                case (ushort)PacketType.Ping:
                    {
                        PongPacket packet = new PongPacket();
                        SendRelib(packet, client.Id);
                        break;
                    }
                case (ushort)PacketType.Connect:
                    {
                        ConnectReturnPacket packet = new ConnectReturnPacket();
                        SendRelib(packet, client.Id);

                        //scene sync when connect
                        SceneSyncStart packet2 = new SceneSyncStart();
                        SendRelib(packet2, client.Id);
                        break;
                    }
                case (ushort)PacketType.SceneSyncClear:
                    {
                        ParticleSpawnPacket packet = new ParticleSpawnPacket();
                        for (int i = 0; i < ParticleChunkManager.chunks.Length; i++)
                        {
                            for (int j = 0; j < ParticleChunkManager.chunks[i].particles.Length; j++)
                            {
                                if(ParticleChunkManager.chunks[i].particles[j].id.particleIndex == -1)
                                    continue;
                                packet.position = ParticleChunkManager.chunks[i].particles[j].position;
                                packet.name = ParticleChunkManager.chunks[i].particles[j].GetParticleProperties().name;
                                SendRelib(packet, client.Id);
                            }
                        }
                        
                        break;
                    }
                case (ushort)PacketType.Player:
                    {
                        //send this packet to all clients except from sender
                        PlayerPacket packet = new PlayerPacket();
                        packet.ByteToPacket(data);
                        if(connectionIds.Count == server.ClientCount)
                        {

                            //if our client id is greater than the one before we subtract 1 from the sent id
                            //if our client id is less than the one after we use the same value
                            uint currentId = connectionIds[client.Id];

                            for (int i = 0; i < connectionIds.Count; i++)
                            {
                                //if we are the same client we skip
                                if (i == currentId)
                                    continue;

                                if (currentId > connectionIds[server.Clients[i].Id])
                                {
                                    packet.id = (int)currentId;
                                    SendUnrelib(packet, server.Clients[i].Id);
                                }
                                else
                                {
                                    packet.id = (int)currentId + 1;
                                    SendUnrelib(packet, server.Clients[i].Id);
                                }
                            }
                        }
                        break;
                    }
                case (ushort)PacketType.PlayerJoin:
                    {
                        //send join signal to all other clients except to the one that joined
                        PlayerJoinPacket packet = new PlayerJoinPacket();
                        packet.ByteToPacket(data);
                        SendRelibAllExclude(packet, client.Id);

                        connectionCount++;

                        //send signals to create a player for the current client if it joined after another client
                        //if (connectionCount == server.ClientCount)
                        //{
                            for (int i = 0; i < connectionCount - 1; i++)
                            {
                                SendRelib(packet, client.Id);
                            }
                        //}
                        break;
                    }
            }
        }

        public bool IsRunning() => server.IsRunning;

    }
}
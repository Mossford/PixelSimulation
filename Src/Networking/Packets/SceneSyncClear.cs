using System.IO;

namespace SpatialEngine.Networking.Packets
{

    public class SceneSyncClear : Packet
    {
        public SceneSyncClear()
        {

        }

        public override byte[] ConvertToByte()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)PacketType.SceneSyncClear);
            stream.Close();
            writer.Close();
            return stream.ToArray();
        }

        public override void ByteToPacket(byte[] data)
        {
            //does nothing
        }

        public override ushort GetPacketType() => (ushort)PacketType.SceneSyncClear;
    }
}
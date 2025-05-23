using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SpatialEngine
{
    public static class Resources
    {
        //public static string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string resourcePath = "res/";
        public static string ShaderPath = resourcePath + "Shaders/";
        public static string ImagePath = resourcePath + "Images/";
        public static string ModelPath = resourcePath + "Models/";
        public static string SimPath = resourcePath + "Sim/";
        public static string FontPath = resourcePath + "Font/";

        //IMPORTANT
        //IF PUBLISHED IN SINGLE FILE DOTNET CANNOT FIND THE PATH OF EXECUTION AS REFLECTION DOES NOT WORK FOR SINGLE FILE PUBLISHING
        public static void InitResources()
        {
            /*if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                appPath = "";
                resourcePath = "res/";
                ShaderPath = resourcePath + "Shaders/";
                ImagePath = resourcePath + "Images/";
                ModelPath = resourcePath + "Models/";
                ScenePath = resourcePath + "Scenes/";
            }*/

            //check if the resource path is there and throw an error if not
            if(!Directory.Exists(resourcePath))
            {
                throw new Exception("Could not find resource directory. Make sure to run with the directory in the same location as the game");
            }
        }
    }
}
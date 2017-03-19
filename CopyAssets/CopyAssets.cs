using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JustForFun
{
    public class CopyAssets
    {
        #region Constants

        private const string SOURCE_PATH_CONF_KEY = "SourcePath";
        private const string DESTINATION_PATH_CONF_KEY = "DestinationPath";
        private const string ACCEPTED_SIZES_CONF_KEY = "AcceptedSizes";

        #endregion

        #region Properties

        /// <summary>
        /// Source Path
        /// </summary>
        private static string SourcePath
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings[SOURCE_PATH_CONF_KEY]);
            }
        }

        /// <summary>
        /// Destination Path
        /// </summary>
        private static string DestinationPath
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(ConfigurationManager.AppSettings[DESTINATION_PATH_CONF_KEY]);
            }
        }

        /// <summary>
        /// Accepted Sizes
        /// </summary>
        private static Size[] AcceptedSizes
        {
            get
            {
                string rawAcceptedSizes = ConfigurationManager.AppSettings[ACCEPTED_SIZES_CONF_KEY];
                string[] rawSizes = rawAcceptedSizes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                List<Size> lstSizes = new List<Size>();
                foreach (string rawSize in rawSizes)
                {
                    string[] sizeParts = rawSize.Split(new char[] { 'x' }, StringSplitOptions.RemoveEmptyEntries);

                    int x, y;
                    if (sizeParts.Length > 1 && Int32.TryParse(sizeParts[0], out x) && Int32.TryParse(sizeParts[1], out y))
                    {
                        lstSizes.Add(new Size(x, y));
                    }
                }

                return lstSizes.ToArray();
            }
        }

        #endregion

        #region Public methods

        public static void Copy()
        {
            //Si no existe creamos carpeta destino
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
            }

            //Obtenemos el hash de todos los ficheros destino, para no duplicar
            Dictionary<string, string> fileHashes = new Dictionary<string, string>();
            foreach (string filePath in Directory.GetFiles(DestinationPath))
            {
                fileHashes.Add(GetMD5Hash(filePath), filePath);
            }

            //Recorremos los ficheros de la carpeta origen
            foreach (string filePath in Directory.GetFiles(SourcePath))
            {
                Console.WriteLine("Procesando archivo \"{0}\"", Path.GetFileName(filePath));
                try
                {
                    using (Image img = Image.FromFile(filePath))
                    {
                        //Si es un archivo de imagen válido, y cumple el tamaño aceptado, continuamos
                        if (AcceptedSizes.Contains(img.Size))
                        {
                            string hash = GetMD5Hash(filePath);

                            if (!fileHashes.ContainsKey(hash))
                            {
                                string pathArchivoDestino = Path.Combine(DestinationPath, DateTime.Now.ToString("yyyyMMddhhmmssfff") + ".jpg");
                                File.Copy(filePath, pathArchivoDestino);
                                Console.WriteLine("OK: Archivo copiado como {0}", pathArchivoDestino);
                            }
                            else
                            {
                                Console.WriteLine("KO: Ya existe un archivo en el destino con hash {0}", hash);
                            }
                        }
                        else
                        {
                            Console.WriteLine("KO: Imagen rechazada por su tamaño ({0}x{1})", img.Width, img.Height);
                        }
                    }
                }
                catch (OutOfMemoryException)
                {
                    Console.WriteLine("KO: Archivo de imagen no válido");
                }
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Getds the MD5 file hash
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static string GetMD5Hash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        #endregion
    }
}

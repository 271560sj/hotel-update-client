using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HotelUpdateService.update.utils
{
    class ZipHelper
    {
        /// <summary>
        /// 打包压缩文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stream"></param>
        /// <param name="staticPath"></param>
        /// <returns></returns>
        #region public static bool zipDirectory(String path, ZipOutputStream stream, String staticPath)
        public static bool zipDirectory(String path, ZipOutputStream stream, String staticPath)
        {
            bool result = false;
            Crc32 crc = new Crc32();
            try
            {
                String[] files = Directory.GetFileSystemEntries(path);
                if (files.Length <= 0)
                {
                    result = true;
                }
                foreach (String file in files)
                {
                    if (Directory.Exists(file))
                    {
                        if (file.Equals(String.Format(@"{0}\Debug", path)))
                        {
                            continue;
                        }
                        result = zipDirectory(file, stream, staticPath);
                        if (!result)
                        {
                            break;
                        }
                    }
                    else
                    {
                        FileStream fileStream = File.OpenRead(file);

                        byte[] buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, buffer.Length);
                        String tempFile = file.Substring(staticPath.LastIndexOf(@"\") + 1);
                        ZipEntry entry = new ZipEntry(tempFile);

                        entry.DateTime = DateTime.Now;
                        entry.Size = fileStream.Length;
                        fileStream.Close();
                        crc.Reset();
                        crc.Update(buffer);
                        entry.Crc = crc.Value;
                        stream.PutNextEntry(entry);
                        stream.Write(buffer, 0, buffer.Length);
                        result = true;
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (IOException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 解压缩文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static bool unZipFile(String path)
        public static bool unZipFile(String path)
        {
            bool result = false;

            if (!File.Exists(path))
            {
                Logger.info(typeof(ZipHelper), "file not exists.");
                return result;
            }

            try
            {
                using (ZipInputStream zis = new ZipInputStream(File.OpenRead(path)))
                {
                    ZipEntry entry;
                    while ((entry = zis.GetNextEntry()) != null)
                    {
                        Logger.info(typeof(ZipHelper), String.Format("unzip {0}", entry.Name));
                        string directoryName = Path.GetDirectoryName(entry.Name);
                        string fileName = Path.GetFileName(entry.Name);

                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(String.Format(@"back\{0}", directoryName));
                        }

                        if (!String.IsNullOrEmpty(fileName))
                        {
                            using (FileStream fis = File.Create(String.Format(@"back\{0}", entry.Name)))
                            {
                                byte[] data = new byte[1024 * 10];
                                while (true)
                                {
                                    int size = zis.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        fis.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            catch (IOException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (NotSupportedException ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 加压缩tar文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        #region public static bool unTarFile(String path, String directory)
        public static bool unTarFile(String path, String directory)
        {
            bool result = false;
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(directory))
            {
                Logger.info(typeof(ZipHelper), "tar file path is empty.");
                return result;
            }

            if (!File.Exists(path))
            {
                Logger.info(typeof(ZipHelper), "tar file not exist.");
                return result;
            }

            try
            {
                using (TarInputStream tis = new TarInputStream(File.OpenRead(path)))
                {
                    TarEntry entry = null;
                    while((entry = tis.GetNextEntry()) != null)
                    {
                        Logger.info(typeof(ZipHelper), String.Format("untar {0}", entry.Name));
                        String parent = Path.GetDirectoryName(entry.Name);
                        String name = Path.GetFileName(entry.Name);
                        if(parent.Length > 0)
                        {
                            Directory.CreateDirectory(String.Format(@"{0}\{1}", directory, parent));
                        }
                        if (!String.IsNullOrEmpty(name))
                        {
                            using (FileStream fis = File.Create(String.Format(@"{0}\{1}", directory,entry.Name)))
                            {
                                byte[] data = new byte[1024 * 10];
                                while (true)
                                {
                                    int size = tis.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        fis.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            catch(Exception ex)
            {
                Logger.error(typeof(ZipHelper), ex);
            }
            return result;
            
        }
        #endregion
    }
}

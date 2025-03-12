using System;
using System.IO;

namespace FileManager
{
    public class TestFileAccess
    {
        public static void Main()
        {
            try
            {
                Console.WriteLine("文件系统访问测试");
                Console.WriteLine("----------------");

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Console.WriteLine($"用户目录: {userProfile}");

                if (Directory.Exists(userProfile))
                {
                    Console.WriteLine("用户目录存在");
                    
                    string[] entries = Directory.GetFileSystemEntries(userProfile);
                    Console.WriteLine($"找到 {entries.Length} 个项目:");
                    
                    foreach (string entry in entries)
                    {
                        try
                        {
                            FileAttributes attr = File.GetAttributes(entry);
                            bool isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                            string type = isDirectory ? "文件夹" : "文件";
                            string name = Path.GetFileName(entry);
                            
                            Console.WriteLine($"- {name} ({type})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"- 无法访问: {Path.GetFileName(entry)}, 错误: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("用户目录不存在");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
} 
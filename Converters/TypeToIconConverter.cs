using System;
using System.Globalization;
using System.Windows.Data;

namespace SimpleFileManager2.Converters
{
    public class TypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type switch
                {
                    "文件夹" => "📁",
                    "文本文件" => "📄",
                    "图片" => "🖼️",
                    "音频" => "🎵",
                    "视频" => "🎬",
                    "压缩文件" => "📦",
                    "可执行文件" => "⚙️",
                    "错误" => "❌",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
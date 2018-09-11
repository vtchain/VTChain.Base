using System.Text;

namespace VTChain.Base.Common
{
    public class StringHelper
    {
        public static string GetSubString(string source, int length)
        {
            string result = source;
            if (source.Length > length && length > 3)
            {
                result = source.Substring(0, length - 3);
                result += "...";
            }

            return result;
        }

        /// <summary>
        /// 字符串长度(按字节算)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int StrLength(string str)
        {
            int len = 0;
            byte[] b;

            for (int i = 0; i < str.Length; i++)
            {
                b = Encoding.Default.GetBytes(str.Substring(i, 1));
                if (b.Length > 1)
                    len += 2;
                else
                    len++;
            }

            return len;
        }

        /// <summary>
        /// 截取指定长度字符串(按字节算)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string StrCut(string str, int length, bool bAddTail = true)
        {
            int len = 0;
            byte[] b;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                b = Encoding.Default.GetBytes(str.Substring(i, 1));
                if (b.Length > 1)
                    len += 2;
                else
                    len++;

                if (len >= length)
                    break;

                sb.Append(str[i]);
            }

            if (StrLength(str) > length - 2)
            {
                if (bAddTail)
                {
                    sb.Append("..");
                }
            }





            return sb.ToString();
        }

    }
}

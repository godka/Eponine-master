using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace Eponine_master
{
    interface GeneratorInterface
    {
        byte[] Generate(int index, int max);
    }
    public class SimpleHeartBreakGenerator:GeneratorInterface
    {
        public SimpleHeartBreakGenerator(object obj)
        {

        }
        private void WriteStream(MemoryStream memorystream, short t)
        {
            var tmplength = System.BitConverter.GetBytes(t);
            memorystream.Write(tmplength, 0, tmplength.Length);
        }
        public byte[] Generate(int index, int max)
        {

            MemoryStream memorystream = new MemoryStream();
            WriteStream(memorystream, (short)0);
            return memorystream.ToArray();
        }
    }
    public class SimpleGenerator:GeneratorInterface
    {
        private string mfilename = string.Empty;
        private byte[] buffer = null;
        private byte[] tmpfilename = null;
        public SimpleGenerator(object obj)
        {
            mfilename = (string)obj;
        }
        private void WriteStream(MemoryStream memorystream, short t)
        {
            var tmplength = System.BitConverter.GetBytes(t);
            memorystream.Write(tmplength, 0, tmplength.Length);
        }
        private void WriteStream(MemoryStream memorystream, int t)
        {
            var tmplength = System.BitConverter.GetBytes(t);
            memorystream.Write(tmplength, 0, tmplength.Length);
        }
        public byte[] Generate(int index, int max)
        {
            if (mfilename.Equals(string.Empty))
                return null;
            if (!File.Exists(mfilename))
                return null;

            if (buffer == null)
            {
                buffer = File.ReadAllBytes(mfilename);
            }
            MemoryStream memorystream = new MemoryStream();
            WriteStream(memorystream, (short)1);
            WriteStream(memorystream,buffer.Length);
            WriteStream(memorystream,index);
            WriteStream(memorystream, max);
            if (tmpfilename == null)
            {
                tmpfilename = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(mfilename));
            }
            byte[] tmpfile = new byte[30];
            Array.Clear(tmpfile, 0, 30);
            Array.Copy(tmpfilename, tmpfile, tmpfilename.Length);
            memorystream.Write(tmpfile, 0, tmpfile.Length);
            memorystream.Write(buffer, 0, buffer.Length);

            return memorystream.ToArray();
        }
    }
}

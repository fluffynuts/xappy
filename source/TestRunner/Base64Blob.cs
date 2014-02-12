using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunner
{
    public class Base64Blob
    {

        public byte[] ToBinary()
        {
            lock (this)
            {
                if (_binaryData == null)
                    this.GenerateBinaryBlob();
                return _binaryData;
            }
        }

        public override string ToString()
        {
            lock (this)
            {
                if (_stringData == null)
                {
                    this.GenerateStringData();
                }
                return _stringData;
            }
        }

        private void GenerateStringData()
        {
            _stringData = Convert.ToBase64String(_binaryData);
        }

        private void GenerateBinaryBlob()
        {
            _binaryData = Convert.FromBase64String(_stringData);
        }

        private byte[] _binaryData;
        private string _stringData;

        public Base64Blob(string base64Data)
        {
            _stringData = base64Data;
            _binaryData = null;
        }

        public Base64Blob(byte[] base64Data)
        {
            _binaryData = base64Data;
            _stringData = null;
        }
    }
}

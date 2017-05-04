using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace d360.extensions
{
    public interface IMediaProvider
    {
        void UploadAsset(string path);
    }
}

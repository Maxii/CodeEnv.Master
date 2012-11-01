using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeEnv.Master.Common.MyEnums;
using CodeEnv.Master.Common.Utility;


namespace Program {
    class PictureList {

        IList<string> picList = null;
        int index = 0;

        internal void Init() {
            picList = System.IO.Directory.GetFiles(@"C:\Users\Public\Pictures\Sample Pictures", "*.jpg");
            // proves GetListFromString works
            picList = Utility.GetListFromString(string.Join(",", picList));
            // proves exception can draw message from Resources
            //  picList = Utility.GetListFromString(null);
        }

        internal string peek() {
            return picList[index];
        }

        internal string Previous() {
            if (index == 0) {
                index = picList.Count;
            }
            index--;
            return picList[index];
        }

        internal string Next() {
            if (index == (picList.Count - 1)) {
                index = 0;
            }
            index++;
            return picList[index];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using static Stitch.InputNameSpace.Tokenizer;

namespace Stitch {
    namespace MMCIFNameSpace {
        public static class MMCIFTokenizer {
            /// <summary> Tokenize the given file into the custom key value file format. </summary>
            /// <param name="file">The file to tokenize. </param>
            /// <returns> If everything went smoothly a list with all top level key value pairs, otherwise a list of error messages. </returns>
            public static ParseResult<DataBlock> Tokenize(ParsedFile file) {
                var pointer = new FilePointer(file);

                pointer.TrimCommentsAndWhitespace();
                return pointer.ParseDataBlock();
            }
        }
    }
}
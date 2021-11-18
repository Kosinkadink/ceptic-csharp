using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Iteration
{
    class DataStreamFrameGenerator
    {
        private readonly Guid streamId;
        private readonly byte[] data;
        private bool isFirstHeader;
        private bool isResponse;
        private readonly int frameSize;

        private int index;

        public DataStreamFrameGenerator(Guid streamId, byte[] data, int frameSize, bool isFirstHeader, bool isResponse)
        {
            this.streamId = streamId;
            this.data = data;
            this.frameSize = frameSize / 2; // cut in half for generator to account for possible encoding size increase
            this.isFirstHeader = isFirstHeader;
            this.isResponse = isResponse;
        }

        private bool HasNext()
        {
            return data.Length - 1 > index;
        }

        public IEnumerable<StreamFrame> Frames()
        {
            while (true)
            {
                // get chunk of data
                var chunkSize = Math.Min(frameSize, data.Length - index);
                var chunk = new byte[chunkSize];
                Array.Copy(data, index, chunk, 0, chunkSize);
                // iterate next chunk's starting index
                index += frameSize;
                // if next chunk wil be out of bounds, yield last frame
                if (!HasNext())
                {
                    if (isFirstHeader)
                        yield return StreamFrameHelper.CreateHeaderLast(streamId, chunk);
                    else if (isResponse)
                        yield return StreamFrameHelper.CreateResponseLast(streamId, chunk);
                    else
                        yield return StreamFrameHelper.CreateDataLast(streamId, chunk);
                    // break out of loop, since now done
                    break;
                }
                // otherwise yield continued frames
                if (isFirstHeader)
                {
                    isFirstHeader = false;
                    yield return StreamFrameHelper.CreateHeaderContinued(streamId, chunk);
                }
                else if (isResponse)
                {
                    isResponse = false;
                    yield return StreamFrameHelper.CreateResponseContinued(streamId, chunk);
                }
                else
                    yield return StreamFrameHelper.CreateDataContinued(streamId, chunk);
            }
        }
    }
}

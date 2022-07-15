/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;

namespace Mirage.Serialization
{
    public sealed class Vector3Packer
    {
        private readonly FloatPacker xPacker;
        private readonly FloatPacker yPacker;
        private readonly FloatPacker zPacker;

        public Vector3Packer(float xMax, float yMax, float zMax, int xBitCount, int yBitCount, int zBitCount)
        {
            this.xPacker = new FloatPacker(xMax, xBitCount);
            this.yPacker = new FloatPacker(yMax, yBitCount);
            this.zPacker = new FloatPacker(zMax, zBitCount);
        }
        public Vector3Packer(float xMax, float yMax, float zMax, float xPrecision, float yPrecision, float zPrecision)
        {
            this.xPacker = new FloatPacker(xMax, xPrecision);
            this.yPacker = new FloatPacker(yMax, yPrecision);
            this.zPacker = new FloatPacker(zMax, zPrecision);
        }
        public Vector3Packer(Vector3 max, Vector3 precision)
        {
            this.xPacker = new FloatPacker(max.x, precision.x);
            this.yPacker = new FloatPacker(max.y, precision.y);
            this.zPacker = new FloatPacker(max.z, precision.z);
        }

        public void Pack(NetworkWriter writer, Vector3 value)
        {
            this.xPacker.Pack(writer, value.x);
            this.yPacker.Pack(writer, value.y);
            this.zPacker.Pack(writer, value.z);
        }

        public Vector3 Unpack(NetworkReader reader)
        {
            Vector3 value = default;
            value.x = this.xPacker.Unpack(reader);
            value.y = this.yPacker.Unpack(reader);
            value.z = this.zPacker.Unpack(reader);
            return value;
        }
    }
}

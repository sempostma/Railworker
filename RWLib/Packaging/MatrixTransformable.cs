using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RWLib.Packaging
{
    public class MatrixTransformable
    {
        [JsonPropertyName("moveX")]
        public float MoveX { get; set; }
        [JsonPropertyName("moveY")]
        public float MoveY { get; set; }
        [JsonPropertyName("moveZ")]
        public float MoveZ { get; set; }
        [JsonPropertyName("scaleX")]
        public float ScaleX { get; set; }
        [JsonPropertyName("scaleY")]
        public float ScaleY { get; set; }
        [JsonPropertyName("scaleZ")]
        public float ScaleZ { get; set; }
        [JsonPropertyName("rotateX")]
        public float RotateX { get; set; }
        [JsonPropertyName("rotateY")]
        public float RotateY { get; set; }
        [JsonPropertyName("rotateZ")]
        public float RotateZ { get; set; }

        public MatrixTransformable AddTransformable(MatrixTransformable subject)
        {
            return new MatrixTransformable
            {
                MoveX = this.MoveX + subject.MoveX,
                MoveY = this.MoveY + subject.MoveY,
                MoveZ = this.MoveZ + subject.MoveZ,
                ScaleX = this.ScaleX + subject.ScaleX,
                ScaleY = this.ScaleY + subject.ScaleY,
                ScaleZ = this.ScaleZ + subject.ScaleZ,
                RotateX = this.RotateX + subject.RotateX,
                RotateY = this.RotateY + subject.RotateY,
                RotateZ = this.RotateZ + subject.RotateZ
            };
        }

        public MatrixTransformable InvertZAxis(bool invert)
        {
            var clone = (MatrixTransformable)MemberwiseClone();
            if (invert) clone.MoveZ = -clone.MoveZ;
            return clone;
        }
    }
}

using System.Collections.Generic;

namespace KaedePhi.Core.KaedePhi
{
    public abstract class ControlBase
    {
        public Easing Easing { get; set; } = new(1);

        public float X { get; set; }

        public abstract ControlBase Clone();
    }

    public sealed class AlphaControl : ControlBase
    {
        public float Alpha { get; set; } = 1.0f;

        public static List<AlphaControl> Default
        {
            get
            {
                return new List<AlphaControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Alpha = 1.0f,
                        X = 0.0f
                    },
                    new()
                    {
                        Easing = new Easing(1),
                        Alpha = 1.0f,
                        X = 9999999.0f
                    }
                }.ConvertAll(input => input.Clone() as AlphaControl);
            }
        }

        public override ControlBase Clone()
        {
            // 深拷贝
            return new AlphaControl()
            {
                Easing = new Easing(Easing),
                X = X,
                Alpha = Alpha
            };
        }
    }

    public sealed class XControl : ControlBase
    {
        public float Pos { get; set; } = 1.0f;

        public static List<XControl> Default
        {
            get
            {
                return new List<XControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Pos = 1.0f,
                        X = 0.0f
                    },
                    new()
                    {
                        Easing = new Easing(1),
                        Pos = 1.0f,
                        X = 9999999.0f
                    }
                }.ConvertAll(input => input.Clone() as XControl);
            }
        }

        public override ControlBase Clone()
        {
            // 深拷贝
            return new XControl()
            {
                Easing = new Easing(Easing),
                X = X,
                Pos = Pos
            };
        }
    }

    public sealed class SizeControl : ControlBase
    {
        public float Size { get; set; } = 1.0f;

        public static List<SizeControl> Default
        {
            get
            {
                return new List<SizeControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Size = 1.0f,
                        X = 0.0f
                    },
                    new()
                    {
                        Easing = new Easing(1),
                        Size = 1.0f,
                        X = 9999999.0f
                    }
                }.ConvertAll(input => input.Clone() as SizeControl);
            }
        }

        public override ControlBase Clone()
        {
            // 深拷贝
            return new SizeControl()
            {
                Easing = new Easing(Easing),
                X = X,
                Size = Size
            };
        }
    }

    public sealed class SkewControl : ControlBase
    {
        public float Skew { get; set; } = 1.0f;

        public static List<SkewControl> Default
        {
            get
            {
                return new List<SkewControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Skew = 0.0f,
                        X = 0.0f
                    },
                    new()
                    {
                        Easing = new Easing(1),
                        Skew = 0.0f,
                        X = 9999999.0f
                    }
                }.ConvertAll(input => input.Clone() as SkewControl);
            }
        }

        public override ControlBase Clone()
        {
            // 深拷贝
            return new SkewControl()
            {
                Easing = new Easing(Easing),
                X = X,
                Skew = Skew
            };
        }
    }

    public sealed class YControl : ControlBase
    {
        public float Y { get; set; } = 1.0f;

        public static List<YControl> Default
        {
            get
            {
                return new List<YControl>
                {
                    new()
                    {
                        Easing = new Easing(1),
                        Y = 1.0f,
                        X = 0.0f
                    },
                    new()
                    {
                        Easing = new Easing(1),
                        Y = 1.0f,
                        X = 9999999.0f
                    }
                }.ConvertAll(input => input.Clone() as YControl);
            }
        }

        public override ControlBase Clone()
        {
            // 深拷贝
            return new YControl()
            {
                Easing = new Easing(Easing),
                X = X,
                Y = Y
            };
        }
    }
}

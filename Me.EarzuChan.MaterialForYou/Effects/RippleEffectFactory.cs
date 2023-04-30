using System;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Me.EarzuChan.MaterialForYou.Effects
{
    public static class RippleEffectFactory
    {
        public static MaterialEffect Create()
        {
            var effect = new MaterialEffect()
            {
                Effects = new MaterialEffect.MaterialEffectBlob[]
                {
                    new ("",0,new ColorAnimation()
                        {
                            Duration=new(TimeSpan.FromMilliseconds(200))
                        }
                    )
                    {
                        Field =""
                    }
                }
            };

            return effect;
        }
    }
}

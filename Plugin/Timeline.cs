using System;
using System.Drawing;

namespace Plugin
{
    public class Timeline
    {
        private double time = 0;
        public double Time
        {
            get => time;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException("Time must be between 0 and 1.");
                }
                time = value;
            }
        }

        public void Render(Graphics graphics, RectangleF bounds)
        {

        }
    }
}

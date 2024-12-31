using System;



public class HolgerDemo // DotPeek: The class will be replaced with class Submission\u00230
{
   public string TemperatureToColor(double temperature)
   {
      if (temperature < 0)
      {
          return "#FFFFFF";
      }
      return "#000000";
   }
}

public string TemperatureToColor(double temperature)
{
   switch (temperature)
   {
      case < -15:
         return Colors.Blue.Default;

      case >= -15 and < -14:
         return Colors.Blue.Accent2;

      case >= -14 and < -13:
         return Colors.Blue.Accent3;

      case >= -13 and < -12:
         return Colors.Blue.Accent4;

      case >= -12 and < -11:
         return Colors.LightBlue.Default;

      case >= -11 and < -10:
         return Colors.LightBlue.Accent2;

      case >= -10 and < -9:
         return Colors.LightBlue.Accent3;

      case >= -9 and < -8:
         return Colors.LightBlue.Accent4;

      case >= -8 and < -7:
         return Colors.Cyan.Default;

      case >= -7 and < -6:
         return Colors.Cyan.Accent2;

      case >= -6 and < -5:
         return Colors.Cyan.Accent3;

      case >= -5 and < -4:
         return Colors.Cyan.Accent4;

      case >= -4 and < -3:
         return Colors.Teal.Default;

      case >= -3 and < -2:
         return Colors.Teal.Accent2;

      case >= -2 and < -1:
         return Colors.Teal.Accent3;

      case >= -1 and < 0:
         return Colors.Teal.Accent4;

      case >= 0 and < 1:
         return Colors.Green.Default;

      case >= 1 and < 2:
         return Colors.Green.Accent2;

      case >= 2 and < 3:
         return Colors.Green.Accent3;

      case >= 3 and < 4:
         return Colors.Green.Accent4;

      case >= 4 and < 5:
         return Colors.LightGreen.Default;

      case >= 5 and < 6:
         return Colors.LightGreen.Accent2;

      case >= 6 and < 7:
         return Colors.LightGreen.Accent3;

      case >= 7 and < 8:
         return Colors.LightGreen.Accent4;

      case >= 8 and < 9:
         return Colors.Yellow.Default;

      case >= 9 and < 10:
         return Colors.Yellow.Accent2;

      case >= 10 and < 11:
         return Colors.Yellow.Accent3;

      case >= 11 and < 12:
         return Colors.Yellow.Accent4;

      case >= 12 and < 13:
         return Colors.Orange.Default;

      case >= 13 and < 14:
         return Colors.Orange.Accent2;

      case >= 14 and < 15:
         return Colors.Orange.Accent3;

      case >= 15 and < 16:
         return Colors.Orange.Accent4;

      case >= 16 and < 17:
         return Colors.DeepOrange.Default;

      case >= 17 and < 18:
         return Colors.DeepOrange.Accent2;

      case >= 18 and < 19:
         return Colors.DeepOrange.Accent3;

      case >= 19 and < 20:
         return Colors.DeepOrange.Accent4;

      case >= 20 and < 21:
         return Colors.Red.Default;

      case >= 21 and < 22:
         return Colors.Red.Accent2;

      case >= 22 and < 23:
         return Colors.Red.Accent3;

      case >= 23 and < 24:
         return Colors.Red.Accent4;

      case >= 24 and < 25:
         return Colors.Pink.Default;

      case >= 25 and < 26:
         return Colors.Pink.Accent2;

      case >= 26 and < 27:
         return Colors.Pink.Accent3;

      case >= 27 and < 28:
         return Colors.Pink.Accent4;

      case >= 28 and < 29:
         return Colors.Purple.Default;

      case >= 30 and < 31:
         return Colors.Purple.Accent2;

      case >= 31 and < 32:
         return Colors.Purple.Accent3;

      case >= 32 and < 33:
         return Colors.Purple.Accent4;

      default:
         return Colors.DeepPurple.Default;

   }
}



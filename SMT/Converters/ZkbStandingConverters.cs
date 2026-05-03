using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SMT.EVEData;

namespace SMT.Converters
{
    /// <summary>
    /// zKillboard row background from victim alliance vs active character standings.
    /// </summary>
    public class ZKBBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ZKillRedisQ.ZKBDataSimple zs = value as ZKillRedisQ.ZKBDataSimple;
            Color rowCol = (Color)ColorConverter.ConvertFromString("#FF333333");
            if(zs != null)
            {
                float Standing = 0.0f;

                LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if(c != null && c.ESILinked)
                {
                    if(c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if(c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if(Standing == -10.0)
                    {
                        rowCol = Colors.Red;
                    }

                    if(Standing == -5.0)
                    {
                        rowCol = Colors.Orange;
                    }

                    if(Standing == 5.0)
                    {
                        rowCol = Colors.LightBlue;
                    }

                    if(Standing == 10.0)
                    {
                        rowCol = Colors.Blue;
                    }
                }
            }

            return new SolidColorBrush(rowCol);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// zKillboard row foreground from victim alliance vs active character standings.
    /// </summary>
    public class ZKBForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ZKillRedisQ.ZKBDataSimple zs = value as ZKillRedisQ.ZKBDataSimple;
            Color rowCol = Colors.White;
            if(zs != null)
            {
                float Standing = 0.0f;

                LocalCharacter c = MainWindow.AppWindow.RegionUC.ActiveCharacter;
                if(c != null && c.ESILinked)
                {
                    if(c.AllianceID != 0 && c.AllianceID == zs.VictimAllianceID)
                    {
                        Standing = 10.0f;
                    }

                    if(c.Standings.Keys.Contains(zs.VictimAllianceID))
                    {
                        Standing = c.Standings[zs.VictimAllianceID];
                    }

                    if(Standing == -10.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == -5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == 5.0)
                    {
                        rowCol = Colors.Black;
                    }

                    if(Standing == 10.0)
                    {
                        rowCol = Colors.White;
                    }
                }
            }

            return new SolidColorBrush(rowCol);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

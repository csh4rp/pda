using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GongSolutions.Shell;
using GongSolutions.Shell.Interop;
using System.Threading;

namespace ProcessDataArchiver.DataCore.Infrastructure
{
    public class NetworkScanner
    {
        public static IEnumerable<string> GetNetworkDevices()
        {
            ShellItem folder = new ShellItem((Environment.SpecialFolder)CSIDL.NETWORK);
            IEnumerator<ShellItem> e = folder.GetEnumerator(SHCONTF.FOLDERS);

            while (e.MoveNext())
            {
                if (Environment.MachineName == e.Current.ParsingName.Substring(2))
                    yield return "localhost";
                else
                yield return e.Current.ParsingName;
            }
        }

        public static Task<IEnumerable<string>> GetNetworkDevicesAsync()
        {
            return Task.Run(() =>
            {
                IList<string> lista = new List<string>();
                ShellItem folder = new ShellItem((Environment.SpecialFolder)CSIDL.NETWORK);
                IEnumerator<ShellItem> e = folder.GetEnumerator(SHCONTF.FOLDERS);

                while (e.MoveNext())
                {
                    if (Environment.MachineName == e.Current.ParsingName.Substring(2))
                        lista.Add("localhost");
                    else
                        lista.Add(e.Current.ParsingName.Substring(2));
                }
                return (IEnumerable<string>)lista;

            });
        }
        public static Task<IEnumerable<string>> GetNetworkDevicesAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                IList<string> lista = new List<string>();
                ShellItem folder = new ShellItem((Environment.SpecialFolder)CSIDL.NETWORK);
                IEnumerator<ShellItem> e = folder.GetEnumerator(SHCONTF.FOLDERS);
                
                while (e.MoveNext() && !token.IsCancellationRequested)
                {
                    if (Environment.MachineName == e.Current.ParsingName.Substring(2))
                        lista.Add("localhost");
                    else
                        lista.Add(e.Current.ParsingName.Substring(2));
                }
                return (IEnumerable<string>)lista;

            });
        }
    }
}

using ProcessDataArchiver.DataCore.Database.DbProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPDev.Public;
using CPDev.SADlg;
using ProcessDataArchiver.DataCore.Database.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Data.Common;
using ProcessDataArchiver.DataCore.DbEntities;
using System.Xml;
using System.Security.Cryptography;

namespace ProcessDataArchiver.DataCore.Acquisition
{
    [Serializable]
    public class ArchvieProjectInfo
    {
        private const string _encryptKey = "1qaz2wsx3edc4rfv1qaz2wsx3edc4rfv";
        private const string _encryptIV = "1qaz2wsx3edc4rfv";
        [NonSerialized]
        private ICPSim_Target _dataSource;

        [NonSerialized]
        private IDatabaseProvider _dbProvider;

        public string CPDevProgramPath { get; set; }
        public string ArchiveProjectPath { get; set; }

        public string DataSourceName { get; set; }
        public DatabaseType DbType { get; set; }
        public DatabaseType CmdProviderType { get; set; }

        [XmlIgnore]
        public string ConnectionString { get; set; }

        public string EncryptedCs { get; set; }
        public int TaskCycleMS { get; set; }

        [XmlIgnore]
        public ICPSim_Target DataSource
        {
            get { return _dataSource; }
            set { _dataSource = value; }
        }

        [XmlIgnore]
        public IDatabaseProvider DbProvider
        {
            get { return _dbProvider; }
            set { _dbProvider = value; }
        }



        public ArchvieProjectInfo()
        {

        }

        public ArchvieProjectInfo(IDatabaseProvider prov, ICPSim_Target dSource)
        {
            this.DbProvider = prov;
            this.ConnectionString = this.DbProvider.ConnectionString;
            this.DbType = this.DbProvider.DatabaseType;
            this.CmdProviderType = this.DbProvider.CmdProviderType;
            this.DataSource = dSource;
            this.DataSourceName = this.DataSource.Name;
        }

        public IEnumerable<GlobalVariable> ReadVariables()
        {
            List<GlobalVariable> variables = new List<GlobalVariable>();


            XmlReader reader = XmlReader.Create(CPDevProgramPath);
            try
            {
                while (reader.Read())
                {
                    if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "VAR"
                        && reader.AttributeCount > 0)
                    {
                        string name = reader.GetAttribute("LName");
                        uint address = Convert.ToUInt32(reader.GetAttribute("Addr"), 16);
                        int size = Int32.Parse(reader.GetAttribute("Size"));
                        Type type = GetVariableType(reader.GetAttribute("Type"));


                        if (name != null && size > 0 && type != null)
                            variables.Add(new GlobalVariable
                            {
                                Address = address,
                                Name = name,
                                Size = size,
                                NetType = type
                            });
                    }

                    else if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "TASK"
                        && reader.AttributeCount > 0)
                    {
                        string name = reader.GetAttribute("LName");
                        string cycleUnit = reader.GetAttribute("CycleUnit");
                        int cycle = Int32.Parse(reader.GetAttribute("Cycle"));

                        if (name != null && cycleUnit != null && cycle > 0)
                        {
                            if (cycleUnit == "ms")
                                TaskCycleMS = cycle ;
                            else if (cycleUnit == "s")
                                TaskCycleMS = cycle*1000;
                        }

                    }

                }
            }
            finally
            {
                reader.Close();
            }
            return variables;
        }

        public static IEnumerable<GlobalVariable> ReadVariables(string path)
        {

            List<GlobalVariable> variables = new List<GlobalVariable>();


            XmlReader reader = XmlReader.Create(path);
            try
            {
                while (reader.Read())
                {
                    if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "VAR"
                        && reader.AttributeCount > 0)
                    {
                        string name = reader.GetAttribute("LName");
                        uint address = Convert.ToUInt32(reader.GetAttribute("Addr"), 16);
                        int size = Int32.Parse(reader.GetAttribute("Size"));
                        Type type = GetVariableType(reader.GetAttribute("Type"));


                        if (name != null && size > 0 && type != null)
                            variables.Add(new GlobalVariable
                            {
                                Address = address,
                                Name = name,
                                Size = size,
                                NetType = type
                            });
                    }

                    else if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "TASK"
                        && reader.AttributeCount > 0)
                    {
                        string name = reader.GetAttribute("LName");
                        string cycleUnit = reader.GetAttribute("CycleUnit");
                        int cycle = Int32.Parse(reader.GetAttribute("Cycle"));


                    }

                }
            }
            finally
            {
                reader.Close();
            }
            return variables;
        }
    



        public void Save()
        {
            EncryptedCs = Encrypt(ConnectionString);
            var xml = new XmlSerializer(typeof(ArchvieProjectInfo));
            using (var fstream = new FileStream(ArchiveProjectPath, FileMode.Create))
            {
                xml.Serialize(fstream, this);
            }
        }

        public void SaveAs(string path)
        {
            EncryptedCs = Encrypt(ConnectionString);

            var xml = new XmlSerializer(typeof(ArchvieProjectInfo));
            using (var fstream = new FileStream(path, FileMode.Create))
            {
                xml.Serialize(fstream, this);
            }
        }
        

        public static ArchvieProjectInfo Load(string path)
        {
            ArchvieProjectInfo arInf;
            var xml = new XmlSerializer(typeof(ArchvieProjectInfo));
            using (var fstream = new FileStream(path, FileMode.Open))
            {
                arInf = (ArchvieProjectInfo)xml.Deserialize(fstream);
            }
            arInf.ConnectionString = ArchvieProjectInfo.Decrypt(arInf.EncryptedCs);
            GlobalDataSources.UpdateTargetList();

            arInf.DbProvider = DatabaseProviderFactory.CreateProvider(arInf.DbType, arInf.ConnectionString
                ,arInf.CmdProviderType);
            arInf.DataSource = GlobalDataSources.Targets
                .Where(t => t.Name.Equals(arInf.DataSourceName, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();

            arInf.DbType = arInf.DbProvider.DatabaseType;
            arInf.CmdProviderType = arInf.DbProvider.CmdProviderType;
            return arInf;
        }

        private static string Encrypt(string plain)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {

                aes.Key = Encoding.UTF8.GetBytes(_encryptKey);
                aes.IV = Encoding.UTF8.GetBytes(_encryptIV);
                ICryptoTransform ct = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] txt = Encoding.UTF8.GetBytes(plain);
                byte[] encrypted = ct.TransformFinalBlock(txt, 0, txt.Length);
                return Convert.ToBase64String(encrypted);
            }
        }

        private static string Decrypt(string encrypted)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptKey);
                aes.IV = Encoding.UTF8.GetBytes(_encryptIV);
                ICryptoTransform ct = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] txt = Convert.FromBase64String(encrypted);
                byte[] decrypted = ct.TransformFinalBlock(txt, 0, txt.Length);
                Console.WriteLine(decrypted[0]);
                return Encoding.UTF8.GetString(decrypted);
            }

        }



        private static Type GetVariableType(string type)
        {
            switch (type)
            {
                case "BOOL":
                    return typeof(bool);
                case "DATE":
                case "TIME":
                case "DATE_AND_TIME":
                case "TIME_OF_DAY":
                    return typeof(DateTime);
                case "SINT":
                    return typeof(sbyte);
                case "INT":
                    return typeof(short);
                case "DINT":
                    return typeof(int);
                case "LINT":
                    return typeof(long);
                case "REAL":
                    return typeof(float);
                case "LREAL":
                    return typeof(double);
                case "BYTE":
                    return typeof(byte);
                case "WORD":
                    return typeof(ushort);
                case "DWORD":
                    return typeof(uint);
                case "LWORD":
                    return typeof(ulong);
                default:
                    throw new ArgumentException("Nieprawidłowa wartość argumentu");

            }
        }
    }
}

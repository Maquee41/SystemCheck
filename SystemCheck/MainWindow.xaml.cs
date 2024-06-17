using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Management;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;

namespace SystemCheck
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnDiagnoseOS_Click(object sender, RoutedEventArgs e)
        {
            string osInfo = GetOSInformation();
            string filePath = SaveToWordDocument("Информация_ОС.docx", "О системе", osInfo);
            MessageBox.Show($"Файл сохранен в {filePath}");
        }

        private void btnDiagnoseEventLog_Click(object sender, RoutedEventArgs e)
        {
            string eventLogInfo = GetEventLogInformation();
            string filePath = SaveToWordDocument("Информация_журнал.docx", "Критические ошибки:", eventLogInfo);
            MessageBox.Show($"Файл сохранен в {filePath}");
        }

        private void btnDiagnoseRegistry_Click(object sender, RoutedEventArgs e)
        {
            string registryInfo = GetRegistryInformation();
            string filePath = SaveToWordDocument("Информация_Реестр.docx", "Реестр | Записи об установленных и удаленных программах", registryInfo);
            MessageBox.Show($"Файл сохранен в {filePath}");
        }

        private void btnDiagnoseInstalledPrograms_Click(object sender, RoutedEventArgs e)
        {
            string installedProgramsInfo = GetInstalledProgramsInformation();
            string filePath = SaveToWordDocument("Информация_Установленные_программы.docx", "Установленные программы:", installedProgramsInfo);
            MessageBox.Show($"Файл сохранен в {filePath}");
        }

        private string GetOSInformation()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"  Операционная система: {Environment.OSVersion}");
            sb.AppendLine($"  Платформа: {Environment.OSVersion.Platform}");
            sb.AppendLine($"  Версия: {Environment.OSVersion.Version}");

            sb.AppendLine($"Имя компьютера: {Environment.MachineName}");

            sb.AppendLine("|Процессор|");
            foreach (var processor in new ManagementObjectSearcher("SELECT ProcessorId, Name, Description, Manufacturer, MaxClockSpeed FROM Win32_Processor").Get())
            {
                sb.AppendLine($"  CPU id: {processor["ProcessorId"]}");
                sb.AppendLine($"  Полное наименование: {processor["Name"]}");
                sb.AppendLine($"  Описание: {processor["Description"]}");
                sb.AppendLine($"  Manufacturer: {processor["Manufacturer"]}");
                sb.AppendLine($"  Частота: {processor["MaxClockSpeed"]} MHz");
            }

            sb.AppendLine("|Оперативная память|");
            foreach (var memory in new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory").Get())
            {
                sb.AppendLine($"  Объем: {Convert.ToInt64(memory["Capacity"]) / 1024 / 1024} Мб");
                sb.AppendLine($"  Частота: {memory["Speed"]} MHz");
            }

            sb.AppendLine("|Накопители|");
            foreach (var disk in new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive").Get())
            {
                sb.AppendLine($"  Модель: {disk["Model"]}");
                sb.AppendLine($"  Объём: {Convert.ToInt64(disk["Size"]) / 1024 / 1024 / 1024} ГБ");
                sb.AppendLine("-----------------------------------");
            }

            return sb.ToString();
        }

        private string GetEventLogInformation()
        {
            EventLog eventLog = new EventLog("System");
            StringBuilder logInfo = new StringBuilder();
            foreach (EventLogEntry entry in eventLog.Entries)
            {
                if (entry.EntryType == EventLogEntryType.Error)
                {
                    logInfo.AppendLine($"{entry.TimeWritten}: {entry.Message}");
                }
            }
            return logInfo.ToString();
        }

        private string GetRegistryInformation()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string keyName in Registry.ClassesRoot.GetSubKeyNames())
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(keyName))
                {
                    if (key != null)
                    {
                        // Собираем информацию о каждом ключе
                        sb.AppendLine($"Ключ: {keyName}");
                        sb.AppendLine("Описание:");
                        sb.Append(key.GetValue("Description"));
                        sb.AppendLine("\nЗначения:");
                        foreach (var valueName in key.GetValueNames())
                        {
                            var value = key.GetValue(valueName);
                            sb.AppendLine($"{valueName}: {value}");
                        }
                        sb.AppendLine("-----------------------------");
                    }
                }
            }
            return sb.ToString();
        }

        private string GetInstalledProgramsInformation()
        {
            StringBuilder installedProgramsInfo = new StringBuilder();
            string registryKey32 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            string registryKey64 = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

            // Получение 64-разрядных программ
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey64))
            {
                foreach (string subkeyName in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                    {
                        string displayName = subkey.GetValue("DisplayName") as string;
                        string displayVersion = subkey.GetValue("DisplayVersion") as string;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            installedProgramsInfo.AppendLine($"{displayName} - {displayVersion}");
                        }
                    }
                }
            }

            // Получение 32-разрядных программ
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey32))
            {
                foreach (string subkeyName in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                    {
                        string displayName = subkey.GetValue("DisplayName") as string;
                        string displayVersion = subkey.GetValue("DisplayVersion") as string;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            installedProgramsInfo.AppendLine($"{displayName} - {displayVersion}");
                        }
                    }
                }
            }

            return installedProgramsInfo.ToString();
        }

        private string SaveToWordDocument(string fileName, string title, string content)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(documentsPath, fileName);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(new Paragraph(new Run(new Text(title))));

                string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    body.Append(new Paragraph(new Run(new Text(line))));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return filePath;
        }
    }
}

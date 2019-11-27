using ELMFilteringApp.MessageFilter;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ELMFilteringApp.ViewModels
{
    public class MainViewModel : NotificationObject
    {
        private ICommand _exportJsonCommand;

        private Dictionary<string, MessageFilterBase> _filterDic = new Dictionary<string, MessageFilterBase>();
        private Dictionary<string, string> _abbreviationDic;
        private string _messagHeader;
        private string _messageType = "Unknown";
        private string _inputMessageBody;
        private string _filteredMessageBody;
        private string _sender;
        private string _subject;
        private int _characterCount;
        private int _maxCharacterCount;
        private Dictionary<string, List<string>> _extraInfos;


        #region Properties
        public Dictionary<string, List<string>> ExtraInfos
        {
            get { return _extraInfos; }
            set
            {
                _extraInfos = value;
                RaisePropertyChanged(nameof(ExtraInfos));
            }
        }


        public int MaxCharacterCount
        {
            get { return _maxCharacterCount; }
            set
            {
                if (_maxCharacterCount != value)
                {
                    _maxCharacterCount = value;
                    RaisePropertyChanged(nameof(MaxCharacterCount));
                }
            }
        }

        public int CharacterCount
        {
            get { return _characterCount; }
            set
            {
                if (_characterCount != value)
                {
                    _characterCount = value;
                    RaisePropertyChanged(nameof(CharacterCount));
                }
            }
        }

        public string Subject
        {
            get { return _subject; }
            set
            {
                if (_subject != value)
                {
                    _subject = value;
                    RaisePropertyChanged(nameof(Subject));
                }
            }
        }


        public string Sender
        {
            get { return _sender; }
            set
            {
                if (_sender != value)
                {
                    _sender = value;
                    RaisePropertyChanged(nameof(Sender));
                }
            }
        }

        public string FilteredMessageBody
        {
            get { return _filteredMessageBody; }
            set
            {
                if (_filteredMessageBody != value)
                {
                    _filteredMessageBody = value;
                    RaisePropertyChanged(nameof(FilteredMessageBody));
                }
            }
        }

        public string InputMessageBody
        {
            get { return _inputMessageBody; }
            set
            {
                if (_inputMessageBody != value)
                {
                    _inputMessageBody = value;
                    RaisePropertyChanged(nameof(InputMessageBody));
                    OnInputMessageBodyChanged();
                }
            }
        }

        public string MessageHeader
        {
            get { return _messagHeader; }
            set
            {
                if (_messagHeader != value)
                {
                    _messagHeader = value;
                    OnMessageHeaderChanged();
                }
            }
        }

        public bool IsEmail
        {
            get
            {
                if (string.IsNullOrEmpty(MessageType))
                    return false;
                return MessageType.StartsWith("Email");
            }
        }


        public string MessageType
        {
            get { return _messageType; }
            set
            {
                if (_messageType != value)
                {
                    _messageType = value;
                    RaisePropertyChanged(nameof(MessageType), nameof(IsEmail));
                }
            }
        }
        #endregion

        #region Commands

        public ICommand ExportToJsonCommand
        {
            get { return _exportJsonCommand ?? (_exportJsonCommand = new DelegateCommand<object>(ExecuteExportToJson)); }
        }

        #endregion

        #region constructor

        public MainViewModel()
        {
            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            _filterDic.Add("S", new SmsMessageFilter());
            _filterDic.Add("E", new EmailMessageFilter());
            _filterDic.Add("T", new TweetMessageFilter());
            LoadAbbreviationDic();
        }

        /// <summary>
        /// load abbreviation dictionary
        /// </summary>
        private void LoadAbbreviationDic()
        {
            _abbreviationDic = new Dictionary<string, string>();
            string csvFilePath = System.Configuration.ConfigurationManager.AppSettings["AbbreviationFile"];
            if (!File.Exists(csvFilePath))
                return;
            using (StreamReader strReader = new StreamReader(csvFilePath, Encoding.Default))
            {
                string line;
                while ((line = strReader.ReadLine()) != null)
                {
                    string[] array = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (array.Length != 2 || string.IsNullOrEmpty(array[0]))
                        continue;
                    _abbreviationDic[array[0]] = array[1];
                }
            }

        }

        #endregion


        #region Private Method

        private void OnMessageHeaderChanged()
        {
            string regStr = @"^[EST]\d{9}$";
            bool match = Regex.IsMatch(MessageHeader, regStr);
            if (!match)
            {
                MessageType = "Unknown Type";
                return;
            }
            switch (MessageHeader[0])
            {
                case 'E':
                    MessageType = "Email";
                    break;
                case 'S':
                    MessageType = "SMS";
                    break;
                case 'T':
                    MessageType = "Tweet";
                    break;
                default:
                    break;
            }
        }

        private void OnInputMessageBodyChanged()
        {
            if (!_filterDic.ContainsKey(MessageType[0].ToString()))
                return;
            var processor = _filterDic[MessageType[0].ToString()];
            processor.ProcessMessage(_abbreviationDic, InputMessageBody.TrimStart(new char[] { ' ' }));

            Sender = processor.Sender;
            FilteredMessageBody = processor.MessageBody;
            CharacterCount = processor.CharacterCount;
            MaxCharacterCount = processor.MaxCharacterCount;
            MessageType = processor.MessageType;
            Subject = processor.GetSubject();
            ExtraInfos = processor.ExtraList;
        }

        /// <summary>
        /// used for background thread to update message body character count
        /// </summary>
        /// <param name="input"></param>
        private void CalculateCharacterCount(string input)
        {
            while (true)
            {
                if (string.IsNullOrEmpty(input))
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                string[] array = input.Split(new char[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { CharacterCount = array.Length; }));
            }
        }

        #endregion

        #region Command Methods

        private void ExecuteExportToJson(object parameter)
        {
            if (!_filterDic.ContainsKey(MessageType[0].ToString()))
            {
                System.Windows.MessageBox.Show("No valid message to export!");
                return;
            }
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Export to Json File";
            saveDialog.Filter = "EVERY FILES(*.*)|*.*";
            var result = saveDialog.ShowDialog(System.Windows.Application.Current.MainWindow);
            if (result != true)
                return;

            try
            {
                
                var processor = _filterDic[MessageType[0].ToString()];
                string jsonStr = JsonConvert.SerializeObject(processor);

                FileStream fs = new FileStream(saveDialog.FileName, FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(jsonStr);
                sw.Dispose();
                fs.Dispose();
                System.Windows.MessageBox.Show("Export to json file succeed!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        #endregion
    }
}

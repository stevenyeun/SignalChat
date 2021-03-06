﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Drawing;
using ChatClientCS.Services;
using ChatClientCS.Enums;
using ChatClientCS.Models;
using ChatClientCS.Commands;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Reactive.Linq;
using Ini;
using System.Windows;
using QlightLibrary;
using System.Threading;
using ChatClientCS.Log;

namespace ChatClientCS.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IChatService chatService;
        private IDialogService dialogService;
        private TaskFactory ctxTaskFactory;
        private const int MAX_IMAGE_WIDTH = 150;
        private const int MAX_IMAGE_HEIGHT = 150;
        private const int MAX_FILE_SIZE = 1048576; //1 MB = 1048576 Byte
        ConsoleIni consoleIni = new ConsoleIni("Setting_Client");
        private static HTTPClientForDB httpclienfordb = new HTTPClientForDB();
        private static log log = new log();
        string mode;

        private string _userName;
        public string UserName
        {
            get
            {
                _userName = consoleIni.id;

                return _userName;
            }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _profilePic;
        public string ProfilePic
        {
            get { return _profilePic; }
            set
            {
                _profilePic = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Participant> _participants = new ObservableCollection<Participant>();
        public ObservableCollection<Participant> Participants
        {
            get { return _participants; }
            set
            {
                _participants = value;
                OnPropertyChanged();
            }
        }

        private Participant _selectedParticipant;
        public Participant SelectedParticipant
        {
            get { return _selectedParticipant; }
            set
            {
                _selectedParticipant = value;
                if (SelectedParticipant.HasSentNewMessage) SelectedParticipant.HasSentNewMessage = false;
                OnPropertyChanged();
            }
        }

        private UserModes _userMode;
        public UserModes UserMode
        {
            get { return _userMode; }
            set
            {
                _userMode = value;
                OnPropertyChanged();
            }
        }

        private string _textMessage;
        public string TextMessage
        {
            get { return _textMessage; }
            set
            {
                _textMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
            }
        }

        #region Connect Command
        private ICommand _connectCommand;
        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommandAsync(() => Connect()));
            }
        }

        private async Task<bool> Connect()
        {
            try
            {
                await chatService.ConnectAsync();
                IsConnected = true;
                return true;
            }
            catch (Exception) { return false; }
        }
        #endregion
        bool OnlyOneThread = false;
        bool myCanLogin = false;
        #region Login Command
        private ICommand _loginCommand;
        public ICommand LoginCommand
        {
            get
            {
                if (OnlyOneThread == false)
                {
                    OnlyOneThread = true;
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            // do the work in the loop
                            string newData = DateTime.Now.ToLongTimeString();

                            // update the UI on the UI thread
                            //Dispatcher.Invoke(() => txtTicks.Text = "TASK - " + newData);

                            // don't run again for at least 200 milliseconds
                            await Task.Delay(200);
                            CanLogin();

                            if (IsLoggedIn) break;

                            if (myCanLogin)
                            {
                                Console.WriteLine("CanLogin {0} ", this.myCanLogin);
                                await Login();
                                break;
                            }
                        }
                    });
                }

                return _loginCommand ?? (_loginCommand =
                    new RelayCommandAsync(() => Login(), (o) => CanLogin()));
            }
        }

        private async Task<bool> Login()
        {
            try
            {
                List<User> users = new List<User>();
                users = await chatService.LoginAsync(_userName, Avatar());
                if (users != null)
                {
                    users.ForEach(u => Participants.Add(new Participant { Name = u.Name, Photo = u.Photo }));
                    UserMode = UserModes.Chat;
                    IsLoggedIn = true;
                    return true;
                }
                else
                {
                    dialogService.ShowNotification("잠시 후 다시 시도바랍니다.");
                    return false;
                }

            }
            catch (Exception) { return false; }
        }

        private bool CanLogin()
        {

            Console.WriteLine("Called CanLogin");
            //true를 리턴하면 로그인버튼 활성화
            if (UserName != null)
            {
                this.myCanLogin = !string.IsNullOrEmpty(UserName) && UserName.Length >= 2 && IsConnected;
            }

            return !string.IsNullOrEmpty(UserName) && UserName.Length >= 2 && IsConnected;
        }
        #endregion

        #region Logout Command
        private ICommand _logoutCommand;
        public ICommand LogoutCommand
        {
            get
            {
                return _logoutCommand ?? (_logoutCommand =
                    new RelayCommandAsync(() => Logout(), (o) => CanLogout()));
            }
        }

        private async Task<bool> Logout()
        {
            try
            {
                await chatService.LogoutAsync();
                UserMode = UserModes.Login;
                return true;
            }
            catch (Exception) { return false; }
        }

        private bool CanLogout()
        {
            return IsConnected && IsLoggedIn;
        }
        #endregion

        #region Typing Command
        private ICommand _typingCommand;
        public ICommand TypingCommand
        {
            get
            {
                return _typingCommand ?? (_typingCommand =
                    new RelayCommandAsync(() => Typing(), (o) => CanUseTypingCommand()));
            }
        }

        private async Task<bool> Typing()
        {
            try
            {
                await chatService.TypingAsync(SelectedParticipant.Name);
                return true;
            }
            catch (Exception) { return false; }
        }

        private bool CanUseTypingCommand()
        {
            return (SelectedParticipant != null && SelectedParticipant.IsLoggedIn);
        }
        #endregion

        #region Send Text Message Command
        private ICommand _sendTextMessageCommand;
        public ICommand SendTextMessageCommand
        {
            get
            {
                return _sendTextMessageCommand ?? (_sendTextMessageCommand =
                    new RelayCommandAsync(() => SendTextMessage(), (o) => CanSendTextMessage()));
            }
        }

        private async Task<bool> SendTextMessage()
        {
            try
            {
                var recepient = _selectedParticipant.Name;
                await chatService.SendUnicastMessageAsync(recepient, _textMessage);
                return true;
            }
            catch (Exception) { return false; }
            finally
            {
                ChatMessage msg = new ChatMessage
                {
                    Author = UserName,
                    Message = _textMessage,
                    Time = DateTime.Now,
                    IsOriginNative = true
                };
                SelectedParticipant.Chatter.Add(msg);

                if(mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, UserName, _selectedParticipant.Name, msg.Message);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(UserName, _selectedParticipant.Name, msg.Message);
                    }
                   ).Start();
                }
                

                TextMessage = string.Empty;
            }
        }

        private bool CanSendTextMessage()
        {
            return (!string.IsNullOrEmpty(TextMessage) && IsConnected &&
                _selectedParticipant != null && _selectedParticipant.IsLoggedIn);
        }
        #endregion

        #region Send Alert Command
        private ICommand _sendAlertCommand;
        public ICommand SendAlertCommand
        {
            get
            {
                return _sendAlertCommand ?? (_sendAlertCommand =
                    new RelayCommandAsync(() => SendAlert(), (o) => CanSendAlert()));
            }
        }

        private async Task<bool> SendAlert()
        {
            try
            {
                var recepient = _selectedParticipant.Name;
                await chatService.SendUnicastMessageAsync(recepient, "!!!!!!!!!!!!!경고발생!!!!!!!!!!!!!", true);
                return true;
            }
            catch (Exception) { return false; }
            finally
            {
                ChatMessage msg = new ChatMessage
                {
                    Author = UserName,
                    Message = "!!!!!!!!!!!!!경고발생!!!!!!!!!!!!!",
                    Time = DateTime.Now,
                    IsOriginNative = true
                };
                SelectedParticipant.Chatter.Add(msg);

                if (mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, UserName, _selectedParticipant.Name, msg.Message);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(UserName, _selectedParticipant.Name, msg.Message);
                    }
                    ).Start();
                }
                TextMessage = string.Empty;
            }
        }

        private bool CanSendAlert()
        {
            return (IsConnected && _selectedParticipant != null && _selectedParticipant.IsLoggedIn);
        }
        #endregion

        #region Send Picture Message Command
        private ICommand _sendImageMessageCommand;
        public ICommand SendImageMessageCommand
        {
            get
            {
                return _sendImageMessageCommand ?? (_sendImageMessageCommand =
                    new RelayCommandAsync(() => SendImageOrVideoMessage(), (o) => CanSendImageMessage()));
            }
        }

        private async Task<bool> SendImageOrVideoMessage()
        {
            //var pic = dialogService.OpenFile("Select image file", "Images (*.jpg;*.png)|*.jpg;*.png");
            var filePathName = dialogService.OpenFile("Select image file", "Images (*.jpg;*.png)|*.jpg;*.png |Videos (*.avi;*.mp4)|*.avi;*.mp4");
            if (string.IsNullOrEmpty(filePathName)) return false;

            var content = await Task.Run(() => File.ReadAllBytes(filePathName));

            int fileSize = content.Length;

            if (fileSize > MAX_FILE_SIZE)
            {
                dialogService.ShowNotification("파일크기 1MB이하만 전송 가능합니다.");
                return false;
            }

            bool isImage = false;
            try
            {
                var recepient = _selectedParticipant.Name;

                string extension = Path.GetExtension(filePathName).Substring(1).ToLower();
                if (extension == "jpg")
                {
                    isImage = true;
                    await chatService.SendUnicastMessageAsync(recepient, content); 
                }
                else//동영상
                {
                    isImage = false;
                    VideoType videoType = VideoType.AVI;
                    switch (extension)
                    {
                        case "avi":
                            videoType = VideoType.AVI;
                            break;
                        case "mp4":
                            videoType = VideoType.MP4;
                            break;
                    }

                    await chatService.SendUnicastMessageAsync(recepient, content, (int)videoType);
                }                
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                if(isImage)
                {
                    ChatMessage msg = new ChatMessage { Author = UserName, Picture = filePathName, Time = DateTime.Now, IsOriginNative = true };
                    SelectedParticipant.Chatter.Add(msg);

                    if (mode == "1")
                    {
                        string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                        string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                        log.WriteFile(strFileName, strDate, UserName, _selectedParticipant.Name, "전송한이미지:" + filePathName);
                    }
                    else
                    {
                        new Thread(() =>
                        {
                            httpclienfordb.DB(UserName, _selectedParticipant.Name, "전송한이미지:" + filePathName);
                        }
                        ).Start();
                    }
                }
                else
                {
                    ChatMessage msg = new ChatMessage { Author = UserName, Message = "전송한동영상:" + filePathName, Video = filePathName, Time = DateTime.Now, IsOriginNative = true };
                    SelectedParticipant.Chatter.Add(msg);

                    if (mode == "1")
                    {
                        string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                        string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                        log.WriteFile(strFileName, strDate, UserName, _selectedParticipant.Name, msg.Message);
                    }
                    else
                    {
                        new Thread(() =>
                        {
                            httpclienfordb.DB(UserName, _selectedParticipant.Name, msg.Message);
                        }
                        ).Start();
                    }
                }

            }           
        }

        private bool CanSendImageMessage()
        {
            return (IsConnected && _selectedParticipant != null && _selectedParticipant.IsLoggedIn);
        }
        #endregion

        #region Select Profile Picture Command
        private ICommand _selectProfilePicCommand;
        public ICommand SelectProfilePicCommand
        {
            get
            {
                return _selectProfilePicCommand ?? (_selectProfilePicCommand =
                    new RelayCommand((o) => SelectProfilePic()));
            }
        }

        private void SelectProfilePic()
        {
            var pic = dialogService.OpenFile("Select image file", "Images (*.jpg;*.png)|*.jpg;*.png");
            if (!string.IsNullOrEmpty(pic))
            {
                var img = Image.FromFile(pic);
                if (img.Width > MAX_IMAGE_WIDTH || img.Height > MAX_IMAGE_HEIGHT)
                {
                    dialogService.ShowNotification($"Image size should be {MAX_IMAGE_WIDTH} x {MAX_IMAGE_HEIGHT} or less.");
                    return;
                }
                ProfilePic = pic;
            }
        }
        #endregion

        #region Open Image Command
        private ICommand _openImageCommand;
        public ICommand OpenImageCommand
        {
            get
            {
                return _openImageCommand ?? (_openImageCommand =
                    new RelayCommand<ChatMessage>((m) => OpenImage(m)));
            }
        }

        private void OpenImage(ChatMessage msg)
        {
            var img = msg.Picture;
            if (string.IsNullOrEmpty(img) || !File.Exists(img)) return;
            try
            {
                Process.Start(img);
            }
            catch(Exception e)
            {

            }
        }
        #endregion

        #region Open Video Command
        private ICommand _openVideoCommand;
        public ICommand OpenVideoCommand
        {
            get
            {
                return _openVideoCommand ?? (_openVideoCommand =
                    new RelayCommand<ChatMessage>((m) => OpenVideo(m)));
            }
        }

        private void OpenVideo(ChatMessage msg)
        {
            var img = msg.Video;
            if (string.IsNullOrEmpty(img) || !File.Exists(img)) return;
            try
            {
                Process.Start(img);
            }
            catch (Exception e)
            {

            }
        }
        #endregion

        #region Change Cursor Command
        private ICommand _mouseEnterCommand;
        public ICommand MouseEnterCommand
        {
            get
            {
                return _mouseEnterCommand ?? (_mouseEnterCommand =
                    new RelayCommand<ChatMessage>((m) => ChangeCursor(m, true)));
            }
        }
        private ICommand _mouseLeaveCommand;
        public ICommand MouseLeaveCommand
        {
            get
            {
                return _mouseLeaveCommand ?? (_mouseLeaveCommand =
                    new RelayCommand<ChatMessage>((m) => ChangeCursor(m, false)));
            }
        }
        private void ChangeCursor(ChatMessage msg, bool mouseEnter)
        {
            if(!mouseEnter)//MouseLeave
                Mouse.OverrideCursor = null;
            else//MouseEnter
            {
                var img = msg.Video;
                if (string.IsNullOrEmpty(img) || !File.Exists(img))
                {
                    Mouse.OverrideCursor = null;
                    return;
                }
                Mouse.OverrideCursor = Cursors.Hand;
            }
        }
        #endregion

        #region Event Handlers
        private void NewTextMessage(string name, string msg, MessageType mt)
        {
            if (mt == MessageType.Unicast)
            {
                ChatMessage cm = new ChatMessage { Author = name, Message = msg, Time = DateTime.Now };
                var sender = _participants.Where((u) => string.Equals(u.Name, name)).FirstOrDefault();
                ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

                if (!(SelectedParticipant != null && sender.Name.Equals(SelectedParticipant.Name)))
                {
                    ctxTaskFactory.StartNew(() => sender.HasSentNewMessage = true).Wait();
                }

                if (mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, name, UserName, msg);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(name, UserName, msg);
                    }
                   ).Start();
                }
            }
        }

        private void NewAlertMessage(string name, string msg, bool alert_flag, MessageType mt)
        {
            MediaPlayer Mysound = new MediaPlayer();

            //var uri = new Uri("pack://application:,,,/alert.mp3");

            //using (FileStream soundFile = File.Create("test.mp3"))
            //{
            //    Application.GetResourceStream(uri).Stream.CopyTo(soundFile);
            //}

            if (mt == MessageType.Unicast)
            {
                if (alert_flag == true)
                {
                    Mysound.Open(new Uri("./alert.mp3", UriKind.Relative));
                    Mysound.Volume = 10;
                    Mysound.Play();
                    //Console.Beep();
                }

                ChatMessage cm = new ChatMessage { Author = name, Message = msg, Time = DateTime.Now };
                var sender = _participants.Where((u) => string.Equals(u.Name, name)).FirstOrDefault();
                ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

                if (!(SelectedParticipant != null && sender.Name.Equals(SelectedParticipant.Name)))
                {
                    ctxTaskFactory.StartNew(() => sender.HasSentNewMessage = true).Wait();
                }

                //n초후에 램프 정지
                LampController.LampOn();
                Observable.Timer(TimeSpan.FromMilliseconds(5000)).Subscribe(
                    t => LampController.LampOff()
                    );

                if (mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, name, UserName, msg);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(name, UserName, msg);
                    }
                   ).Start();
                }

            }
        }

        private void NewImageMessage(string name, byte[] pic, MessageType mt)
        {
            if (mt == MessageType.Unicast)
            {
                var imgsDirectory = Path.Combine(Environment.CurrentDirectory, "Image Messages");
                if (!Directory.Exists(imgsDirectory)) Directory.CreateDirectory(imgsDirectory);

                var imgsCount = Directory.EnumerateFiles(imgsDirectory).Count() + 1;
                var imgPath = Path.Combine(imgsDirectory, $"IMG_{imgsCount}.jpg");

                ImageConverter converter = new ImageConverter();
                using (Image img = (Image)converter.ConvertFrom(pic))
                {
                    img.Save(imgPath);
                }

                ChatMessage cm = new ChatMessage { Author = name, Picture = imgPath, Time = DateTime.Now };
                var sender = _participants.Where(u => string.Equals(u.Name, name)).FirstOrDefault();
                ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

                if (!(SelectedParticipant != null && sender.Name.Equals(SelectedParticipant.Name)))
                {
                    ctxTaskFactory.StartNew(() => sender.HasSentNewMessage = true).Wait();
                }

                if (mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, name, UserName, "수신된이미지:" + imgPath);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(name, UserName, "수신된이미지:" + imgPath);
                    }
                   ).Start();
                }
            }
        }

        private void NewVideoMessage(string name, byte[] video, int videoType, MessageType mt)//동영상수신
        {
            if (mt == MessageType.Unicast)
            {
                var videosDirectory = Path.Combine(Environment.CurrentDirectory, "Video Messages");
                if (!Directory.Exists(videosDirectory)) Directory.CreateDirectory(videosDirectory);

                var videosCount = Directory.EnumerateFiles(videosDirectory).Count() + 1;

                string extension = Enum.GetName(typeof(VideoType), videoType).ToLower();
                var imgPath = Path.Combine(videosDirectory, $"VIDEO_{videosCount}."+ extension);

                try
                {
                    File.WriteAllBytes(imgPath, video);
                }
                catch (Exception e)
                {
                    // Exception ...
                }

                ChatMessage cm = new ChatMessage { Author = name, Message = "수신된동영상:"+imgPath, Video = imgPath, Time = DateTime.Now };
                var sender = _participants.Where(u => string.Equals(u.Name, name)).FirstOrDefault();
                ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

                if (!(SelectedParticipant != null && sender.Name.Equals(SelectedParticipant.Name)))
                {
                    ctxTaskFactory.StartNew(() => sender.HasSentNewMessage = true).Wait();
                }

                if (mode == "1")
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + ".txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    log.WriteFile(strFileName, strDate, name, UserName, cm.Message);
                }
                else
                {
                    new Thread(() =>
                    {
                        httpclienfordb.DB(name, UserName, cm.Message);
                    }
                   ).Start();
                }

            }
        }

        private void ParticipantLogin(User u)
        {
            var ptp = Participants.FirstOrDefault(p => string.Equals(p.Name, u.Name));
            if (_isLoggedIn && ptp == null)
            {
                ctxTaskFactory.StartNew(() => Participants.Add(new Participant
                {
                    Name = u.Name,
                    Photo = u.Photo
                })).Wait();
            }
            else
            {
                try
                {
                    ptp.IsLoggedIn = true;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void ParticipantDisconnection(string name)
        {
            var person = Participants.Where((p) => string.Equals(p.Name, name)).FirstOrDefault();
            if (person != null) person.IsLoggedIn = false;
        }

        private void ParticipantReconnection(string name)
        {
            var person = Participants.Where((p) => string.Equals(p.Name, name)).FirstOrDefault();
            if (person != null) person.IsLoggedIn = true;
        }

        private void Reconnecting()
        {
            IsConnected = false;
            IsLoggedIn = false;
        }

        private async void Reconnected()
        {
            var pic = Avatar();
            if (!string.IsNullOrEmpty(_userName)) await chatService.LoginAsync(_userName, pic);
            IsConnected = true;
            IsLoggedIn = true;
        }

        private async void Disconnected()
        {
            var connectionTask = chatService.ConnectAsync();
            await connectionTask.ContinueWith(t => {
                if (!t.IsFaulted)
                {
                    IsConnected = true;
                    IsLoggedIn = true;
                    Login();
                    //chatService.LoginAsync(_userName, Avatar()).Wait();
                    
                }
            });
        }

        private void ParticipantTyping(string name)
        {
            var person = Participants.Where((p) => string.Equals(p.Name, name)).FirstOrDefault();
            if (person != null && !person.IsTyping)
            {
                person.IsTyping = true;
                Observable.Timer(TimeSpan.FromMilliseconds(1500)).Subscribe(t => person.IsTyping = false);
            }
        }
#endregion

        private byte[] Avatar()
        {
            byte[] pic = null;
            if (!string.IsNullOrEmpty(_profilePic)) pic = File.ReadAllBytes(_profilePic);
            return pic;
        }

        public MainWindowViewModel(IChatService chatSvc, IDialogService diagSvc)
        {
            consoleIni.ReadIni();

            mode = consoleIni.mode;

            dialogService = diagSvc;
            chatService = chatSvc;

            chatSvc.NewTextMessage += NewTextMessage;
            chatSvc.NewImageMessage += NewImageMessage;
            chatSvc.NewVideoMessage += NewVideoMessage;
            chatSvc.NewAlertMessage += NewAlertMessage;
            chatSvc.ParticipantLoggedIn += ParticipantLogin;
            chatSvc.ParticipantLoggedOut += ParticipantDisconnection;
            chatSvc.ParticipantDisconnected += ParticipantDisconnection;
            chatSvc.ParticipantReconnected += ParticipantReconnection;
            chatSvc.ParticipantTyping += ParticipantTyping;
            chatSvc.ConnectionReconnecting += Reconnecting;
            chatSvc.ConnectionReconnected += Reconnected;
            chatSvc.ConnectionClosed += Disconnected;

            ctxTaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        }

    }
}
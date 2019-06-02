using System;

namespace ChatServerCS
{
    public interface IClient
    {
        void ParticipantDisconnection(string name);
        void ParticipantReconnection(string name);
        void ParticipantLogin(User client);
        void ParticipantLogout(string name);
        void BroadcastTextMessage(string sender, string message);
        void BroadcastPictureMessage(string sender, byte[] img);
        void UnicastTextMessage(string sender, string message);
        void UnicastPictureMessage(string sender, byte[] img);
        void UnicastVideoMessage(string sender, byte[] video, int videoType);
        void UnicastAlertMessage(string sender, string message, bool alert_flag);        
        void ParticipantTyping(string sender);
    }
}
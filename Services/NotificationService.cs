using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace SmartCity_BE.Services
{
    public class NotificationService
    {
        private readonly FirebaseMessaging _messaging;

        public NotificationService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
                });
            }

            _messaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task SendNotificationAsync(
            string fcmToken,
            string title,
            string body,
            Dictionary<string, string> data)
        {
            try
            {
                var message = new Message()
                {
                    Token = fcmToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig()
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification()
                        {
                            ChannelId = "smartcity_notifications",
                            Sound = "default"
                        }
                    },
                    Apns = new ApnsConfig()
                    {
                        Aps = new Aps()
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    }
                };

                string response = await _messaging.SendAsync(message);
                Console.WriteLine($"✅ Successfully sent message: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending notification: {ex.Message}");
                throw;
            }
        }
    }
}
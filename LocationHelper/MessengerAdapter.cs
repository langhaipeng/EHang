using EHang.Messaging;
using GalaSoft.MvvmLight.Messaging;
using System;

namespace CopterHelper
{
    public class MessengerAdapter : IEHMessenger
    {
        private IMessenger _messenger;

        public MessengerAdapter(IMessenger messenger)
        {
            _messenger = messenger;
        }

        public void Register<TMessage>(object recipient, Action<TMessage> action) => _messenger.Register(recipient, action);

        public void Send<TMessage>(TMessage message) => _messenger.Send(message);

        public void Unregister(object recipient) => _messenger.Unregister(recipient);
    }
}

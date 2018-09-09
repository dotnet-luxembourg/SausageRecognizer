using System;
using System.Windows.Input;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using MvvmHelpers;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace SausageRecognizer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private MediaFile file;

        private ImageSource imageSource;
        public ImageSource ImageSource
        {
            get
            {
                return imageSource;
            }
            set
            {
                SetProperty(ref imageSource, value);
            }
        }

        public ICommand TakePhotoCommand { get; }
        public ICommand PickPhotoCommand { get; }
        public ICommand SendPhotoCommand { get; }

        private readonly CloudStorageAccount storageAccount;

        public MainViewModel()
        {
            TakePhotoCommand = new Command(async () =>
            {
                file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "test.jpg"
                });

                DisplayPhoto();
            });

            PickPhotoCommand = new Command(async () =>
            {
                file = await CrossMedia.Current.PickPhotoAsync();

                DisplayPhoto();
            });

            storageAccount = CloudStorageAccount.Parse(Constants.StorageConnectionString);

            SendPhotoCommand = new Command(async () =>
            {
                IsBusy = true;

                Guid recordId = Guid.NewGuid();

                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(Constants.StorageContainerName);
                var blockBlob = container.GetBlockBlobReference(recordId.ToString());
                await blockBlob.UploadFromStreamAsync(file.GetStream());

                // notify through queue
                var queueClient = storageAccount.CreateCloudQueueClient();
                var queue = queueClient.GetQueueReference(Constants.StorageQueueName);
                await queue.AddMessageAsync(new CloudQueueMessage(recordId.ToString()));

                await Application.Current.MainPage.DisplayAlert("Information", "Your photo has been sent!", "OK");

                IsBusy = false;
            });
        }

        private void DisplayPhoto()
        {
            if (file == null)
                return;
                
            ImageSource = ImageSource.FromStream(file.GetStream);
        }
    }
}

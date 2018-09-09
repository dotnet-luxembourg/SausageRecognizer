using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
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
        public ICommand PredictCommand { get; }

        private readonly PredictionEndpoint predictionEndpoint;

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

            predictionEndpoint = new PredictionEndpoint
            {
                ApiKey = Constants.CustomVisionPredictionKey
            };

            PredictCommand = new Command(async () =>
            {
                IsBusy = true;

                var result = await predictionEndpoint.PredictImageAsync(Guid.Parse(Constants.CustomVisionProjectId), file.GetStream());

                string message = string.Join(Environment.NewLine,
                    result.Predictions
                        .OrderByDescending(p => p.Probability)
                        .Select(p => $"{p.TagName} : {p.Probability * 100}%"));

                await Application.Current.MainPage.DisplayAlert("Information", message, "OK");

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

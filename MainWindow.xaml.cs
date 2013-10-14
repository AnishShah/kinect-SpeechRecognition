using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Speech.Synthesis;
using System.IO;


namespace KinectSpeechRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        KinectSensor myKinect;
        RecognizerInfo kinectRecognizerInfo;
        SpeechRecognitionEngine recognizer;

        KinectAudioSource kinectSource;

        Stream audioStream;
        SpeechSynthesizer speaker;
        private RecognizerInfo findKinectRecognizerInfo()
        {
            var recognizers = SpeechRecognitionEngine.InstalledRecognizers();

            foreach (RecognizerInfo recInfo in recognizers)
            {
                // look at each recognizer info value to find the one that works for Kinect
                if (recInfo.AdditionalInfo.ContainsKey("Kinect"))
                {
                    string details = recInfo.AdditionalInfo["Kinect"];
                    if (details == "True" && recInfo.Culture.Name == "en-US")
                    {
                        // If we get here we have found the info we want to use
                        return recInfo;
                    }
                }
            }
            return null;
        }


        private void createSpeechEngine()
        {
            kinectRecognizerInfo = findKinectRecognizerInfo();

            if (kinectRecognizerInfo == null)
            {
                setupSpeechOutput("Kinect recognizer not found");
                Application.Current.Shutdown();
                return;
            }

            try
            {
                recognizer = new SpeechRecognitionEngine(kinectRecognizerInfo);
            }
            catch
            {
                setupSpeechOutput("Speech recognition engine could not be loaded");
                Application.Current.Shutdown();
            }
        }

        private void buildCommands()
        {
            Choices commands = new Choices();
            commands.Add("What is your name?");

            GrammarBuilder grammarBuilder = new GrammarBuilder();

            grammarBuilder.Culture = kinectRecognizerInfo.Culture;
            grammarBuilder.Append(commands);

            Grammar grammar = new Grammar(grammarBuilder);

            recognizer.LoadGrammar(grammar);
        }

        private void setupAudio()
        {
            try
            {
                myKinect = KinectSensor.KinectSensors[0];
                myKinect.Start();

                kinectSource = myKinect.AudioSource;
                kinectSource.BeamAngleMode = BeamAngleMode.Adaptive;
                audioStream = kinectSource.Start();
                recognizer.SetInputToAudioStream(audioStream, new SpeechAudioFormatInfo(
                                                      EncodingFormat.Pcm, 16000, 16, 1,
                                                      32000, 2, null));
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch
            {
                setupSpeechOutput("Audio stream could not be connected");
                Application.Current.Shutdown();
            }
        }

        private void SetupSpeechRecognition()
        {
            createSpeechEngine();

            buildCommands();

            setupAudio();
            setupSpeechOutput("Ready to Go!");
            recognizer.SpeechRecognized +=
                new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
        }      

        void setupSpeechOutput(string command)
        {
            speaker = new SpeechSynthesizer();
            speaker.Speak(command);
        }

        void shutdownSpeechOutput()
        {
            speaker.Dispose();
        }

        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.9f)
            {
                if (e.Result.Text == "What is your name?")
                {
                    wordTextBlock.Text = "My name is Alfred";
                    setupSpeechOutput("My name is Alfred");
                }
                
            }
            
              
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetupSpeechRecognition();
        }
    }
}

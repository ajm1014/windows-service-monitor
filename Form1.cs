using System;
using System.Collections.Generic;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;

namespace ServiceMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            // Initialize the form
            InitializeComponent();

            // Collection of all service objects
            List<Service> services = new List<Service>();

            // Variables to reference for GUI sizes
            int refreshInterval = 1000;
            int labelLength = 145;
            int textBoxLength = 95;
            int startButtonLength = 45;
            int stopButtonLength = 45; 
            int startXCord = 10;
            int startYCord = 10;
            int xCordChange = 5;
            int yCordChange = 35;
            int componentHeight = 25;

            // Application settings from config file
            NameValueCollection appSettings = ConfigurationManager.AppSettings;

            // Get the maximum label length based on the services entered in appSettings
            Label measureLabel = new Label();
            for (int i = 0; i < appSettings.Count; i++)
            {
                var key = appSettings.GetKey(i);
                if (key.ToString() != "Refresh Interval")
                {
                    measureLabel.Text = key.ToString();
                }
                
                using (Graphics g = CreateGraphics())
                {
                    SizeF size = g.MeasureString(measureLabel.Text, measureLabel.Font);
                    if (labelLength < (int)Math.Ceiling(size.Width))
                    {
                        labelLength = (int)Math.Ceiling(size.Width);
                    }
                }
            }

            // Import information from each key in appSettings
            for (int i = 0; i < appSettings.Count; i++)
            {
                try
                {
                    // Get the next key
                    var key = appSettings.GetKey(i);

                    // If the key is for the refresh interval set the value accordingly, otherwise pull the service information
                    if (key.ToString() == "Refresh Interval")
                    {
                        refreshInterval = int.Parse(appSettings[key].ToString());
                    }
                    else
                    {
                        var serviceInfo = ConfigurationManager.AppSettings[i].Split(',');
                        var serviceName = serviceInfo[0];
                        var computerName = serviceInfo[1];
                        var displayName = appSettings.GetKey(i).ToString();

                        // Create a new service object to keep track of all of the properties and GUI objects related to this service
                        services.Add(new Service
                        {
                            DisplayName = displayName,
                            ServiceController = new ServiceController(serviceName, computerName),
                            Label = new Label
                            {
                                Location = new Point(startXCord, startYCord + 4),
                                Size = new Size(labelLength, componentHeight),
                                Text = displayName
                            },
                            TextBox = new TextBox
                            {
                                Location = new Point((startXCord + labelLength + xCordChange), startYCord + 2),
                                Size = new Size(textBoxLength, componentHeight),
                                TextAlign = HorizontalAlignment.Center,
                                ReadOnly = true,
                                Enabled = false,
                            },
                            StartButton = new Button
                            {
                                Location = new Point((startXCord + labelLength + xCordChange + textBoxLength + xCordChange), startYCord),
                                Size = new Size(startButtonLength, componentHeight),
                                Text = "Start",
                            },
                            StopButton = new Button
                            {
                                Location = new Point((startXCord + labelLength + xCordChange + textBoxLength + xCordChange + startButtonLength + xCordChange), startYCord),
                                Size = new Size(stopButtonLength, componentHeight),
                                Text = "Stop",
                            }
                        });

                        // Set the tag for the button to the service object so the MouseClick methods can reference it
                        services[i].StartButton.Tag = services[i];
                        services[i].StopButton.Tag = services[i];
                        services[i].StartButton.MouseClick += StartButton_MouseClick;
                        services[i].StopButton.MouseClick += StopButton_MouseClick;

                        // Get the service status and set colors and fonts to appropriate colors
                        if (services[i].ServiceController.Status == ServiceControllerStatus.Running)
                        {
                            services[i].TextBox.BackColor = Color.LightGreen;
                            services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                            services[i].StartButton.Enabled = false;
                            services[i].StopButton.Enabled = true;
                        }
                        else if (services[i].ServiceController.Status == ServiceControllerStatus.Stopped)
                        {
                            services[i].TextBox.BackColor = Color.LightCoral;
                            services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                            services[i].StartButton.Enabled = true;
                            services[i].StopButton.Enabled = false;
                        }
                        else
                        {
                            services[i].TextBox.BackColor = Color.LightYellow;
                            services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                            services[i].StartButton.Enabled = false;
                            services[i].StopButton.Enabled = false;
                        }

                        // Add the GUI objects to the form
                        this.Controls.Add(services[i].Label);
                        this.Controls.Add(services[i].TextBox);
                        this.Controls.Add(services[i].StartButton);
                        this.Controls.Add(services[i].StopButton);
                        startYCord += yCordChange;

                    }
                }
                catch { }
            }

            // Set the window width to fit all components and deselect all fields
            this.Width = (startXCord * 3) + (xCordChange * 4) + labelLength + textBoxLength + startButtonLength + stopButtonLength;
            services[0].Label.Select();

            // Start a timer to run the service status refresh
            Timer myTimer = new Timer
            {
                Tag = services,
                Interval = refreshInterval
            };
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Start();
        }


        // Handle click of the start button for a service
        private void StartButton_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                Service serv = ((Service)((Button)sender).Tag);

                // Start the service and update the text box to starting
                serv.ServiceController.Start();
                serv.TextBox.BackColor = Color.LightYellow;
                serv.TextBox.Text = "Starting";

                // Disable the start button and enable the stop button
                serv.StartButton.Enabled = false;
                serv.StopButton.Enabled = true;

                // Remove focus from any text boxes and buttons
                serv.Label.Select();
            }
            catch { }
        }


        // Handle click of the stop button for a service
        private void StopButton_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                Service serv = ((Service)((Button)sender).Tag);

                // Stop the service and update the text box to stopping
                serv.TextBox.BackColor = Color.LightYellow;
                serv.ServiceController.Stop();
                serv.TextBox.Text = "Stopping";

                // Disable the stop button and enable the start button
                serv.StartButton.Enabled = true;
                serv.StopButton.Enabled = false;

                // Remove focus from any text boxes and buttons
                serv.Label.Select();
            }
            catch { }
        }


        // Handle each timer tick
        private void TimerEventProcessor(object sender, EventArgs e)
        {
            // Get a list of the services we are monitoring and loop through them
            List<Service> services = ((List<Service>)((Timer)sender).Tag);
            for (int i = 0; i < services.Count; i++)
            {
                try
                {
                    // Refresh the current status of each service and set the text box and button availability as appropriate
                    services[i].ServiceController.Refresh();
                    if (services[i].ServiceController.Status == ServiceControllerStatus.Running)
                    {
                        services[i].TextBox.BackColor = Color.LightGreen;
                        services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                        services[i].StartButton.Enabled = false;
                        services[i].StopButton.Enabled = true;
                    }
                    else if (services[i].ServiceController.Status == ServiceControllerStatus.Stopped)
                    {
                        services[i].TextBox.BackColor = Color.LightCoral;
                        services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                        services[i].StartButton.Enabled = true;
                        services[i].StopButton.Enabled = false;
                    }
                    else
                    {
                        services[i].TextBox.BackColor = Color.LightYellow;
                        services[i].TextBox.Text = services[i].ServiceController.Status.ToString();
                        services[i].StartButton.Enabled = false;
                        services[i].StopButton.Enabled = false;
                    }
                }
                catch { }
            }
        }


        // Contains all necessary information about each service and the GUI objects associated with it
        struct Service
        {
            public string DisplayName;
            public ServiceController ServiceController;
            public System.Windows.Forms.Label Label;
            public System.Windows.Forms.TextBox TextBox;
            public System.Windows.Forms.Button StartButton;
            public System.Windows.Forms.Button StopButton;
        }
    }
}

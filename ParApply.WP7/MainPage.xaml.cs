﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace ParApply
{
    public partial class MainPage : PhoneApplicationPage
    {
        private GeoCoordinateWatcher _coordinateWatcher;  
        private YrService _yrService;
        private ParaplyService _paraplyService;
        private GeoCoordinate _myLocation;
        private static Noreg _norge;
        private Sted _sted;
        private BackgroundWorker _backgroundWorker;
        private NorgeParser _norgeParser;
        private bool _isFirstLookup = false;
        private bool _parsingComplete;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            _coordinateWatcher = new GeoCoordinateWatcher();
            _coordinateWatcher.PositionChanged += SaveCurrentPosition;
            _coordinateWatcher.Start(false);
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += ParseNorgeFile;
            _backgroundWorker.RunWorkerCompleted += SetParsingCompleted;
            _norgeParser = new NorgeParser();
            
            _paraplyService = new ParaplyService();
            _yrService = new YrService();
             _backgroundWorker.RunWorkerAsync();
        }

  

        private void ParseNorgeFile(object sender, DoWorkEventArgs e)
        {
            using (var stream = ResourceHelper.Noreg())
            {
                _norge = _norgeParser.Parse(stream);
            }
        }

        private void SetParsingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _parsingComplete = true;
            TryUpdateData();
        }

        private void TryUpdateData()
        {
            if(_parsingComplete && _myLocation != null)
            {
                _sted = _norge.FindClosestSted(_myLocation);
                _yrService.GetYrData(_sted, UpdateUI);
            }
        }


        private void SaveCurrentPosition(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            _isFirstLookup = _myLocation == null;
            _myLocation = e.Position.Location;

            if (_isFirstLookup)
            {   
                _coordinateWatcher.Stop();
                _coordinateWatcher.Dispose();
            }
            TryUpdateData();
        }

   



        private void UpdateUI(Result<IEnumerable<YrData>> yrResult)
        {
            var useParaply = _paraplyService.ShouldUseParaply(yrResult);
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => SetImage(useParaply));
        }

        private void SetImage(UseParaplyResult useParaply)
        {
            if(!useParaply.HasError())
            {
                StedInfoTextBlock.Text = string.Format("{0}, {1}, {2} ", _sted.Navn, useParaply.YrData.SymbolName, useParaply.YrData.GetPeriode());    
            }
            
            switch (useParaply.Result)
            {
                case UseParaply.Unknown:
                    ParaplyImage.Source = ResourceHelper.QuestionMark();
                    break;
                case UseParaply.Yes:
                    ParaplyImage.Source = ResourceHelper.UseUmbrella();
                    break;
                case UseParaply.No:
                    ParaplyImage.Source = ResourceHelper.DontUseUmbrella();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }
}
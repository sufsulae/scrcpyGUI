using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace scrcpyGUI
{
    // Interaction logic for Win.xaml
    public partial class Win : Window
    {
        //Private Field
        private List<ItemOption> m_optionItems;
        private BackgroundWorker m_worker;

        //Child Class
        class ItemOption
        {
            public string tag;
            public FrameworkElement parent;
            public List<FrameworkElement> childs;
            public Func<string> validateFunc;

            public ItemOption(FrameworkElement parent)
            {
                this.parent = parent;
                childs = new List<FrameworkElement>();
            }
            public List<FrameworkElement> getAllItemIncludingChildren()
            {
                var res = new List<FrameworkElement>();
                res.Add(parent);
                res.AddRange(childs);
                return res;
            }
        }

        //Constructor
        public Win()
        {
            InitializeComponent();
            m_optionItems = new List<ItemOption>();

            //Register Every Option into single Array For Easy Management
            m_optionItems.Add(new ItemOption(CB_FullScreen) { tag = "Fullscreen", validateFunc = () => {
                return Sys.paramFullScreen;
            } });
            m_optionItems.Add(new ItemOption(CB_ShowTouch) { tag = "ShowTouch", validateFunc = () => {
                return Sys.paramShowTouches;
            } });
            m_optionItems.Add(new ItemOption(CB_AlwaysTop) { tag = "AlwaysTop", validateFunc = () => {
                return Sys.paramAlwaysOnTop;
            } });
            m_optionItems.Add(new ItemOption(CB_NoCtrl) { tag = "NoCtrl", validateFunc = () => {
                return Sys.paramNoControl;
            } });
            m_optionItems.Add(new ItemOption(CB_RenderExpFrame) { tag = "RenderExpFrame", validateFunc = () => {
                return Sys.paramFlag;
            } });

            var newItem = new ItemOption(CB_BitRate);
            newItem.tag = "BitRate";
            newItem.childs.Add(TB_ConfigBitRate);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramBitRate, TB_ConfigBitRate.Text);
            };
            m_optionItems.Add(newItem);

            newItem = new ItemOption(CB_Crop);
            newItem.tag = "Crop";
            newItem.childs.Add(TB_ConfigCropX);
            newItem.childs.Add(TB_ConfigCropY);
            newItem.childs.Add(TB_ConfigCropW);
            newItem.childs.Add(TB_ConfigCropH);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramCrop, TB_ConfigCropY.Text, TB_ConfigCropY.Text, TB_ConfigCropW.Text, TB_ConfigCropH.Text);
            };
            m_optionItems.Add(newItem);

            newItem = new ItemOption(CB_Record);
            newItem.tag = "Record";
            newItem.childs.Add(CB_ConfigRecordNoDisp);
            newItem.childs.Add(COMB_ConfigRecordFmt);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramRecord, "record/srccpy_" + DateTime.Now.ToString() + ".mp4");
            };
            m_optionItems.Add(newItem);

            newItem = new ItemOption(CB_MaxSize);
            newItem.tag = "MaxSize";
            newItem.childs.Add(TB_ConfigMaxSize);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramMaxSize, TB_ConfigMaxSize.Text);
            };
            m_optionItems.Add(newItem);

            newItem = new ItemOption(CB_Port);
            newItem.tag = "Port";
            newItem.childs.Add(TB_ConfigPort);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramPort, TB_ConfigPort.Text);
            };
            m_optionItems.Add(newItem);

            newItem = new ItemOption(CB_Device);
            newItem.tag = "Device";
            newItem.childs.Add(COMB_ConfigDevice);
            newItem.validateFunc = () => {
                return string.Format(Sys.paramSerial, (string)COMB_ConfigDevice.SelectedValue);
            };
            m_optionItems.Add(newItem);

            m_worker = new BackgroundWorker();
            m_worker.WorkerReportsProgress = true;
            m_worker.WorkerSupportsCancellation = true;
            m_worker.DoWork += Worker_DoWork;
            m_worker.ProgressChanged += Worker_ProgressChanged;
            m_worker.RunWorkerAsync();

            Sys.RunChecker();
        }

        //Private Method
        #region Method
        private ItemOption _getOptionItem(string tag) {
            foreach (var i in m_optionItems) {
                if (i.tag == tag)
                    return i;
            }
            return null;
        }
        private StringBuilder _getValidatedOption() {
            var sb = new StringBuilder();
            foreach (var item in m_optionItems) {
                var itemCB = (CheckBox)item.parent;
                var itemCBChecked = (bool)itemCB.IsChecked;
                if (itemCBChecked) {
                    sb.Append(item.validateFunc() + " ");
                }
            }
            var arg = sb.ToString();
            if (!string.IsNullOrEmpty(arg)) {
                Console.WriteLine("Generated Argument: " + sb.ToString());
            }
            Sys.RunProgram(sb);
            return sb;
        }
        #endregion

        //Listener
        #region Listener
        private void BTN_Run_Click(object sender, RoutedEventArgs e) {
            _getValidatedOption();
        }

        private void CB_OnCheckedChanged(object sender, RoutedEventArgs s) {
            var cb = (CheckBox)sender;
            Console.WriteLine(cb.Name + " is Checked: " + cb.IsChecked);
            foreach (var i in m_optionItems) {
                if (i.parent == cb) {
                    foreach (var c in i.childs) {
                        c.IsEnabled = cb.IsChecked.Value;
                    }
                }
            }
        }

        private void TB_OnPreviewInput(object sender, TextCompositionEventArgs e) {
            e.Handled = Util.isStringNumberOnly(e.Text);
        }

        private void TB_OnPreviewInputBitStream(object sender, TextCompositionEventArgs e) {
            var reg = new Regex("[0-9]|m|M|k|K");
            e.Handled = !reg.IsMatch(e.Text);
            if (e.Handled) {
                var regHuruf = new Regex("^(m|M|k|K)");
                if (regHuruf.IsMatch(TB_ConfigBitRate.Text))
                {
                    e.Handled = false;
                }
            }
        }

        private void TB_OnDataPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!Util.isStringNumberOnly(text))
                    goto END;
            }
            END:
                e.CancelCommand();
        }

        private void TB_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void WIN_OnClosing(object sender, CancelEventArgs e) {
            Sys.StopChecker();
            Sys.StopProgram();
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if(Sys.output != null)
                TB_Output.Text = Sys.output.ToString();
            BTN_Run.IsEnabled = !Sys.isRunning;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true) {
                m_worker.ReportProgress(1);
                Thread.Sleep(20);
            }
        }
        #endregion
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using FOHBackend;
using FOHBackend.DoorList;
using FOHBackend.Reports;
using FOHBackend.Roster;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

namespace FOHManagerUI {
    public partial class MainWindow : Form {

        static readonly string LocalVolunteers = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LocalRecords.volunteer");

        static readonly string HomePage = "http://www.trybooking.com/";
        static readonly string Dashboard = "https://portal.trybooking.com/Account/AccountDashboard.aspx";
        static readonly string ExportData = "https://portal.trybooking.com/Reports/ReportTicketHolderCSVExport.aspx";
        static readonly string ManualPage = "https://kintoshmalae.gitbooks.io/foh-management-manual/content/";
        static readonly string EMailPage = "https://zpactheatre.com/webmail";

        ChromiumWebBrowser webBrowser;
        ChromiumWebBrowser manualBrowser;
        ChromiumWebBrowser mailBrowser;
        DownloadHandler downloadHandler;

        public MainWindow() {
            InitializeComponent();
            FOHBackendCallbackManager.registerCallback(new UICallbacks(this));

            printDialog.Document = new DoorListPrinter {
                DocumentName = "doorList"
            };

            webBrowser = new ChromiumWebBrowser(HomePage);
            downloadHandler = new DownloadHandler();
            webBrowser.DownloadHandler = downloadHandler;
            webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            webBrowser.Location = new System.Drawing.Point(13, 13);
            webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            webBrowser.Name = "browserDisplay";
            webBrowser.Size = new System.Drawing.Size(pWebBrowser.Size.Width - 13 - 13, pWebBrowser.Size.Height - 13 - 13);
            // webBrowser.Size = new System.Drawing.Size(1122, 475);
            webBrowser.Dock = DockStyle.Fill;
            webBrowser.TabIndex = 0;
            webBrowser.FrameLoadEnd += automaticLogin;
            // this.Controls.Add(webBrowser);
            pWebBrowser.Controls.Add(webBrowser);

            manualBrowser = new ChromiumWebBrowser(ManualPage) {
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(13, 13),
                MinimumSize = new System.Drawing.Size(20, 20),
                Size = new System.Drawing.Size(pgManual.Size.Width - 13 - 13, pManual.Size.Height - 13 - 13),
                Name = "manualBrowser"
            };
            pManual.Controls.Add(manualBrowser);

            mailBrowser = new ChromiumWebBrowser(EMailPage) {
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(13, 13),
                MinimumSize = new System.Drawing.Size(20, 20),
                Size = new System.Drawing.Size(pgMail.Size.Width - 13 - 13, pgMail.Size.Height - 13 - 13),
                Name = "mailBrowser"
            };
            mailBrowser.FrameLoadEnd += automaticLoginEMail;
            pMail.Controls.Add(mailBrowser);

            // pgMail.SetInvisible();
            pgShowReport.SetInvisible();
            pgRoster.SetInvisible();
            pgVolunteers.SetInvisible();
        }

        void automaticLogin(object sender, EventArgs args) {
            loginUser(webBrowser);
            webBrowser.FrameLoadEnd -= automaticLogin;
        }

        void automaticLoginEMail(object sender, EventArgs args) {
            loginEMailUser(mailBrowser);
            mailBrowser.FrameLoadEnd -= automaticLoginEMail;
        }

        void loginUser(IWebBrowser browser) {
            string uname = FOHBackend.SettingsLoader.Active.TryBooking.Username;
            string pword = FOHBackend.SettingsLoader.Active.TryBooking.Password;
            // string uname = FOHBackend.Settings.ActiveSettings.TryBookingUsername;
            // string pword = FOHBackend.Settings.ActiveSettings.TryBookingPassword;

            browser.EvaluateScriptAsync(FOHBackend.ui.Scripts.getAutologinCommand(uname, pword));
        }

        void loginEMailUser(IWebBrowser browser) {
            string uname = FOHBackend.SettingsLoader.Active.SMTP.Username;
            string pword = FOHBackend.SettingsLoader.Active.SMTP.Password;

            browser.EvaluateScriptAsync(FOHBackend.ui.Scripts.getAutologinEMailCommand(uname, pword));
        }

        void exportData() {
            webBrowser.FrameLoadEnd += automaticExport;
            webBrowser.Load(ExportData);
        }

        /*
        delegate void DCallbackStringArray(IEnumerable<string> values);
        void getItemList(string ctrl, IWebBrowser browser, DCallbackStringArray cback) {
            string cmd = "";
            cmd += "(function() {";
            cmd += "    var _res = [];";
            cmd += "    var ctrl = window['" + ctrl + "'];";
            cmd += "    for (var i = 0; i < ctrl.GetItemCount(); i++) {";
            cmd += "        _res.push(ctrl.GetItem(i).text);";
            cmd += "    }";
            cmd += "    return JSON.stringify(_res);";
            cmd += "})();";
            
            var task = browser.EvaluateScriptAsync(cmd);
            task.ContinueWith(t => {
                if (!t.IsFaulted) {
                    if (t.Result.Success) {
                        dynamic dynObj = JsonConvert.DeserializeObject((string)t.Result.Result);
                        List<string> values = new List<string>();
                        foreach (string s in dynObj) {
                            values.Add(s);
                        }
                        cback(values);
                    } else {
                        cback(null);
                    }
                }
            });
        }
        */

        void allowExport(bool enabled) {
            this.Invoke((Action)(() => { bExport.Enabled = enabled; bExportOnly.Enabled = enabled; }));
        }

        void automaticExport(object sender, EventArgs args) {
            webBrowser.FrameLoadEnd -= automaticExport;
            webBrowser.EvaluateScriptAsync(setExportSettings());
            allowExport(true);
        }

        string setExportSettings() {
            return FOHBackend.ui.Scripts.getExportSettingsCommand();
        }

        private void bSelectExport_Click(Object sender, EventArgs e) {
            exportData();
        }

        private void bExport_Click(Object sender, EventArgs e) {
            doPrintAuto = true;
            downloadHandler.OnDownloadUpdatedFired += onDownload;
            downloadHandler.OnBeforeDownloadFired += onStartDownload;
            webBrowser.ExecuteScriptAsync(FOHBackend.ui.Scripts.getExportCommand());
        }

        private void bExportOnly_Click(Object sender, EventArgs e) {
            doPrintAuto = false;
            downloadHandler.OnDownloadUpdatedFired += onDownload;
            webBrowser.ExecuteScriptAsync(FOHBackend.ui.Scripts.getExportCommand());
        }

        private FileInfo currentDownload;
        public static bool doPrintAuto = false;

        void onStartDownload(object sender, DownloadItem item) {
            string fName = Path.GetTempFileName();
            item.SuggestedFileName = fName;
            currentDownload = new FileInfo(fName);
        }

        void onDownload(object sender, DownloadItem item) {
            if (item.IsComplete) {
                allowExport(false);
                webBrowser.Load(Dashboard);
                downloadHandler.OnDownloadUpdatedFired -= onDownload;
                downloadHandler.OnBeforeDownloadFired -= onStartDownload;
                if (doPrintAuto && currentDownload != null && currentDownload.Exists) {
                    this.Invoke((Action)(() => { doPrint(); }));
                }
            }
        }

        private void bQuit_Click(Object sender, EventArgs e) {
            Application.Exit();
        }

        void doPrint() {
            onPrint(currentDownload.FullName);
            currentDownload.Delete();
            currentDownload = null;
        }

        private void onPrint(string fName) {
            try {
                List<DoorListEntry> list = Helper.loadCSV(fName);

                DoorListPrinter printer = new DoorListPrinter();
                printDialog.Document = printer;
                if (printDialog.ShowDialog(this) == DialogResult.OK) {

                    printer.DocumentName = "doorList-bySurname";
                    printer.listTitle = "Door List by Surname";
                    printer.doorList = Helper.sortByName(list);
                    printer.Print();

                    printer.DocumentName = "doorList-bySeat";
                    printer.listTitle = "Door List by Seat";
                    DoorListEntrySizes sz = printer.doorListSizes;  // cache the already calculated sizes, since these won't change simply by reordering
                    printer.doorList = Helper.sortBySeat(list);
                    printer.doorListSizes = sz; // restore already calculated sizes
                    printer.Print();

                    SeatingMapPrinter seatingMapPrinter = new SeatingMapPrinter();
                    seatingMapPrinter.PrinterSettings = printDialog.PrinterSettings;
                    printDialog.Document = seatingMapPrinter;
                    seatingMapPrinter.doorList = printer.doorList;
                    seatingMapPrinter.Print();
                }
            } catch (Exception) {
                // System.Console.WriteLine(e);
                MessageBox.Show("Error processing file");
            }
        }

        private void bPrint_Click(Object sender, EventArgs e) {
            if (openDialog.ShowDialog(this) == DialogResult.OK) {
                onPrint(openDialog.FileName);
            }
        }

        private void mitmAbout_onClick(Object sender, EventArgs e) {
            AboutBox abt = new AboutBox();
            abt.ShowDialog(this);
        }

        private void mitmExit_onClick(Object sender, EventArgs e) {
            bQuit_Click(sender, e);
        }

        private void mitmTicketing_onClick(Object sender, EventArgs e) {
            tabMainUI.SelectedTab = pgTicketing;
        }

        private void mitmRostering_onClick(Object sender, EventArgs e) {
            tabMainUI.SelectedTab = pgRoster;
        }

        private void mitmVolunteers_onClick(Object sender, EventArgs e) {
            tabMainUI.SelectedTab = pgVolunteers;
        }

        private void bAddVolunteerRecord_Click(Object sender, EventArgs e) {
            VolunteerRecordUI dlg = new VolunteerRecordUI();
            if (dlg.ShowDialog() == DialogResult.OK) {
                lstVolunteerRecords.Items.Add(dlg.baseRecord);
                lstVolunteerRecords.Update();
            }
        }

        private void bEditVolunteerRecord_Click(Object sender, EventArgs e) {
            if (lstVolunteerRecords.SelectedItem == null) {
                MessageBox.Show("Please select the record to be edited first.");
            } else {
                VolunteerRecord oRec = lstVolunteerRecords.SelectedItem as VolunteerRecord;
                if (oRec == null) {
                    MessageBox.Show("Unable to edit this record - it does not appear to be a valid record.");
                    return;
                }
                VolunteerRecordUI dlg = new VolunteerRecordUI(oRec.dup());
                if (dlg.ShowDialog() == DialogResult.OK) {
                    lstVolunteerRecords.BeginUpdate();
                    lstVolunteerRecords.Items.Remove(oRec);
                    lstVolunteerRecords.Items.Add(dlg.baseRecord);
                    lstVolunteerRecords.EndUpdate();
                }
            }
        }

        private void MainWindow_Load(Object sender, EventArgs e) {
            lstVolunteerRecords.BeginUpdate();
            List<VolunteerRecord> _items = VolunteerRecord.loadJSONList(new System.IO.FileInfo(LocalVolunteers));
            foreach (VolunteerRecord rec in _items) lstVolunteerRecords.Items.Add(rec);
            lstVolunteerRecords.EndUpdate();
        }

        private void MainWindow_FormClosing(Object sender, FormClosingEventArgs e) {
            List<VolunteerRecord> _items = new List<VolunteerRecord>();
            foreach (object i in lstVolunteerRecords.Items) {
                if (i is VolunteerRecord _rec) _items.Add(_rec);
            }
            VolunteerRecord.storeJSONList(_items, new System.IO.FileInfo(LocalVolunteers));
        }

        private void bDeleteVolunteerRecord_Click(Object sender, EventArgs e) {
            if (lstVolunteerRecords.SelectedItem == null) {
                MessageBox.Show("Please select the record to be deleted first.");
            } else {
                VolunteerRecord oRec = lstVolunteerRecords.SelectedItem as VolunteerRecord;
                lstVolunteerRecords.BeginUpdate();
                if (oRec == null) {
                    lstVolunteerRecords.Items.Remove(lstVolunteerRecords.SelectedItem);
                    return;
                }
                if (MessageBox.Show(String.Format("Are you sure you want to delete {0}?", oRec), "Delete", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                    lstVolunteerRecords.Items.Remove(oRec);
                }
                lstVolunteerRecords.EndUpdate();
            }
        }

        private void manualToolStripMenuItem_Click(Object sender, EventArgs e) {
            manualBrowser.Load(ManualPage);
            tabMainUI.SelectedTab = pgManual;
        }

        private void mitmOfflineManual_Click(Object sender, EventArgs e) {
            System.Diagnostics.Process.Start(System.IO.Path.Combine(Application.StartupPath, "foh-management-manual.pdf"));
        }

        #region Show Reporting Table Updates

        ShowReport showReport = new ShowReport();

        private void nCashDoorSales_ValueChanged(Object sender, EventArgs e) {
            showReport.CashDoorSales = nCashDoorSales.Value;
            txtTotalDoorSalesUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTDoorSales_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTDoorSales = nEFTDoorSales.Value;
            txtTotalDoorSalesUpdate();
            txtEFTTotalUpdate();
        }

        private void nCashMainBar_ValueChanged(Object sender, EventArgs e) {
            showReport.CashMainBar = nCashMainBar.Value;
            txtTotalMainBarUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTMainBar_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTMainBar = nEFTMainBar.Value;
            txtTotalMainBarUpdate();
            txtEFTTotalUpdate();
        }

        private void nCashWineBar_ValueChanged(Object sender, EventArgs e) {
            showReport.CashWineBar = nCashWineBar.Value;
            txtTotalWineBarUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTWineBar_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTWineBar = nEFTWineBar.Value;
            txtTotalWineBarUpdate();
            txtEFTTotalUpdate();
        }

        private void nCashMember_ValueChanged(Object sender, EventArgs e) {
            showReport.CashMembership = nCashMainBar.Value;
            txtTotalMemberUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTMember_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTMembership = nEFTMember.Value;
            txtTotalMemberUpdate();
            txtEFTTotalUpdate();
        }

        private void nCashKitchen_ValueChanged(Object sender, EventArgs e) {
            showReport.CashKitchen = nCashKitchen.Value;
            txtTotalKitchenUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTKitchen_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTKitchen = nEFTKitchen.Value;
            txtTotalKitchenUpdate();
            txtEFTTotalUpdate();
        }

        private void nCashProgram_ValueChanged(Object sender, EventArgs e) {
            showReport.CashPrograms = nCashProgram.Value;
            txtTotalProgramUpdate();
            txtCashTotalUpdate();
        }

        private void nEFTProgram_ValueChanged(Object sender, EventArgs e) {
            showReport.EFTPrograms = nEFTProgram.Value;
            txtTotalProgramUpdate();
            txtEFTTotalUpdate();
        }

        private void txtTotalDoorSalesUpdate() {
            txtTotalDoorSales.Text = showReport.TotalDoorSales;
        }

        private void txtTotalMainBarUpdate() {
            txtTotalMainBar.Text = showReport.TotalMainBar;
        }

        private void txtTotalWineBarUpdate() {
            txtTotalWineBar.Text = showReport.TotalWineBar;
        }

        private void txtTotalMemberUpdate() {
            txtTotalMember.Text = showReport.TotalMembership;
        }

        private void txtTotalKitchenUpdate() {
            txtTotalKitchen.Text = showReport.TotalKitchen;
        }

        private void txtTotalProgramUpdate() {
            txtTotalProgram.Text = showReport.TotalPrograms;
        }

        private void txtCashTotalUpdate() {
            txtCashTotal.Text = showReport.CashTotal;
        }

        private void txtEFTTotalUpdate() {
            txtEFTTotal.Text = showReport.EFTTotal;
        }

        private void txtGrandTotalUpdate() {
            txtGrandTotal.Text = showReport.GrandTotal;
        }

        private void txtCashTotal_TextChanged(Object sender, EventArgs e) {
            txtGrandTotalUpdate();
        }

        private void txtEFTTotal_TextChanged(Object sender, EventArgs e) {
            txtGrandTotalUpdate();
        }
        #endregion

        private void mitmSettings_Click(Object sender, EventArgs e) {
            SettingsWindow wndSettings = new SettingsWindow(SettingsLoader.Active);
            DialogResult result = wndSettings.ShowDialog(this);
            if (result == DialogResult.OK) {
                SettingsLoader.Active.copyFrom(wndSettings.localSettings);
                try {
                    SettingsLoader.saveSettings();
                } catch (Exception) {
                    MessageBox.Show("Error saving settings");
                }
            }
        }

        private void openConfigFolderToolStripMenuItem_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start(SettingsLoader.SettingsFolder.FullName);
        }
    }

    class UICallbacks : FOHBackend.FOHBackendCallback {

        private MainWindow wndMain;

        public UICallbacks(MainWindow wnd) {
            this.wndMain = wnd;
        }

        public Settingsv3 InitialSettings(Settingsv3 defaultSettings) {
            SettingsWindow wndSettings = new SettingsWindow(defaultSettings) {
                Text = "Initial Settings"
            };
            wndSettings.bCancel.Enabled = false;

            DialogResult result = DialogResult.Ignore;
            if (wndMain.InvokeRequired) {
                wndMain.Invoke((Action)(() => { result = wndSettings.ShowDialog(); }));
            } else {
                result = wndSettings.ShowDialog();
            }
            return (result == DialogResult.OK) ? wndSettings.localSettings : defaultSettings;
        }

        public string RequestString(string msg, string suggestedString) {
            RequestStringWindow wndRequest = new RequestStringWindow(msg, suggestedString);
            DialogResult _result = DialogResult.Ignore;
            if (wndMain.InvokeRequired) {
                wndMain.Invoke((Action)(() => { _result = wndRequest.ShowDialog(); }));
            } else {
                _result = wndRequest.ShowDialog();
            }
            return (_result == DialogResult.OK) ? wndRequest.getText() : suggestedString;
        }

        public void warningMessage(string msg) {
            Console.WriteLine(msg);
            if (wndMain.InvokeRequired) {
                wndMain.Invoke((Action)(() => { MessageBox.Show(msg, "Warning", MessageBoxButtons.OK); }));
            } else {
                MessageBox.Show(msg, "Warning", MessageBoxButtons.OK);
            }
        }
    }
}

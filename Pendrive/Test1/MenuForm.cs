﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;

namespace Test1
{
    public partial class MenuForm : Form
    {
        private Menu appMenu;
        private Hashtable menu;
        private Hashtable categories;
        private MenuUpdater mu;
        private ArrayList shortcutKeys;
        private bool tooLarge;
        Rectangle maxSize = new Rectangle();
        Rectangle resetSize;

        /**
         * Create a new MenuForm, and try to load saved settings.
         */
        public MenuForm(Settings settings, Menu appMenu, MenuUpdater updater)
        {
            this.appMenu = appMenu;
            this.menu = appMenu.getTable();
            this.categories = appMenu.getCategories();
            this.mu = updater;
            InitializeComponent();
            
           
            //Set up logo
            try
            {
                /*Bitmap bmpLogo = new Bitmap("Menu_Data\\logo16.png");
                Icon mainIcon = Icon.FromHandle(bmpLogo.GetHicon());
                //this.Icon = mainIcon;
                Bitmap barLogo = new Bitmap("Menu_Data\\logo256.png");
                Icon barIcon = Icon.FromHandle(barLogo.GetHicon());
                //MessageBox.Show("Icon size: " + barIcon.Size);
                this.Icon = barIcon;
                trayIcon.Icon = barIcon;
                 * */
                Icon mainIcon = new Icon("Menu_Data\\logo.ico");
                this.Icon = mainIcon;
                trayIcon.Icon = mainIcon;
            }
            catch
            {
                //
            }
            //Set up menu treeview
            ImageList imgList = new ImageList();
            Bitmap bmpFolderIcon = new Bitmap("Menu_Data\\icon.png");
            Icon ic = Icon.FromHandle(bmpFolderIcon.GetHicon());
            
            imgList.Images.Add(ic); //adds the Folder icon as a basic icon to be used for categories
            foreach (String cat in categories.Keys)
            {
                appTree.BeginUpdate();
                TreeNode categoryNode = new TreeNode(cat);
                categoryNode.ImageIndex = 0;
                appTree.Nodes.Add(categoryNode); 
                foreach (String app in ((ArrayList)categories[cat])) //gets the ArrayList for each category and iterates through its contained applications
                {
                    try
                    {
                        String path = (String)((AppShortcut)menu[app]).getPath(); //gets the app from the Menu and extracts its path
                        String extra = (String)((AppShortcut)menu[app]).getExtra(); //gets extra information
                        System.Drawing.Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(path); //extracts the associated icon for that app
                        imgList.Images.Add(appIcon);
                        String appTitle = app;
                        if (!extra.Equals("."))
                        {
                            appTitle = app + " (" + extra + ")";
                        }
                        TreeNode appNode = new TreeNode(appTitle);
                        appTree.ImageList = imgList;
                        appTree.ImageIndex = appTree.ImageList.Images.Count;
                        appNode.ImageIndex = appTree.ImageIndex;
                        appNode.SelectedImageIndex = appNode.ImageIndex;
                        appNode.ContextMenuStrip = appTreeContextMenu;
                        categoryNode.Nodes.Add(appNode);
                        ToolStripMenuItem appTrayItem = new ToolStripMenuItem(app);
                        contextMenuStrip1.Items.Add(appTrayItem);
                    }
                    catch
                    {
                        TreeNode appNode = new TreeNode(app);
                        appNode.ImageIndex = 0;
                        appNode.SelectedImageIndex = 0;
                        categoryNode.Nodes.Add(appNode);
                    }
                }
                categoryNode.SelectedImageIndex = 0;                 
            }
            appTree.Sort();
            appTree.EndUpdate();
            ActiveControl = appTree;

            //set up font menu
            System.Drawing.Text.InstalledFontCollection installedFonts = new System.Drawing.Text.InstalledFontCollection();
            ArrayList fontList = new ArrayList();
            fontList.AddRange(installedFonts.Families);
            foreach (FontFamily f in fontList)
            {
                if (f.IsStyleAvailable(FontStyle.Regular))
                {
                    fontToolStripMenuItem.DropDownItems.Add(f.Name); //Adds a menu item for each available font
                }
            }
            for (int i = 10; i <= 70; i += 2)
            {
                sizeToolStripMenuItem.DropDownItems.Add(i.ToString()); //Adds a menu item for each font size 10-70
            }

            //set up shortcut keys
            shortcutKeys = new ArrayList();
            shortcutKeys.Add("CTRL + +, Increase Text Size");
            shortcutKeys.Add("CTRL + -, Decrease Text Size");
            shortcutKeys.Add("CTRL + F, Change Font");
            shortcutKeys.Add("CTRL + D, Set Default Font");
            shortcutKeys.Add("CTRL + T, Change Text Colour");
            shortcutKeys.Add("CTRL + B, Change Background Colour");
            shortcutKeys.Add("CTRL + R, Reverse Colours");
            shortcutKeys.Add("CTRL + [numbers], Change Colour Combinations");
            shortcutKeys.Add("ESC, Reset Colours and Font");
            shortcutKeys.Add("F1, Launch Help File");

            //Set up colours and fonts from settings file
            this.MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
            try
            {
                Color bgColour = ColorTranslator.FromHtml(settings.getBgColour());
                Color fgColour = ColorTranslator.FromHtml(settings.getTxtColour());
                changeBackColour(bgColour);
                changeForeColour(fgColour);

                TypeConverter toFont = TypeDescriptor.GetConverter(typeof(Font));
                Font newFont = (Font)toFont.ConvertFromString(settings.getFont());
                this.Font = new Font(newFont.FontFamily, float.Parse(settings.getFontSize()), newFont.Style, newFont.Unit, newFont.GdiCharSet, newFont.GdiVerticalFont);
                statusLabel1.Font = this.Font;
                menuStrip1.Font = this.Font;
                appTreeContextMenu.Font = this.Font;

                resetSize = new Rectangle();
                resetSize.Width = 295;
                resetSize.Height = 335;
                menuStrip1.Items.Insert(1, new ToolStripSeparator());
                menuStrip1.Items.Insert(3, new ToolStripSeparator());
                appTree.Focus();
            }
            catch(Exception ex)
            {
                CustomBox.Show("There was a problem restoring your settings. The default settings will be used, and a new settings file will be created.", "Error!", this.Font, appTree.BackColor, appTree.ForeColor);
                mu.createSettingsFile();
                this.BringToFront();
                this.Focus();
            }
            checkScreenSize();
            //appTree.ExpandAll();

        }

        /**
         * Saves the current settings and closes the program
         * If the settings can't be saved, then a new settings file is created and another attempt to save is made
         */
        private void saveAndClose()
        {
            try
            {
                mu.saveSettings(ColorTranslator.ToHtml(appTree.BackColor), ColorTranslator.ToHtml(appTree.ForeColor), this.Font.FontFamily.Name.ToString(), this.Font.Size.ToString());
            }
            catch (Exception ex)
            {
                mu.createSettingsFile();
                mu.saveSettings(ColorTranslator.ToHtml(appTree.BackColor), ColorTranslator.ToHtml(appTree.ForeColor), this.Font.FontFamily.Name.ToString(), this.Font.Size.ToString());
            }
            Application.Exit();
        }


        /**
         * Executes the selected application
         */
        private void launchApp()
        {
            if (appTree.SelectedNode != (null))
            {
                char[] extraSplit = "(".ToCharArray();
                String selected = appTree.SelectedNode.Text;
                try
                {
                    selected = selected.Substring(0, selected.LastIndexOfAny(extraSplit) - 1);
                }
                catch
                {

                }
                if (!categories.ContainsKey(selected)) //check the selected item is not a category heading
                {
                    statusLabel1.Text = "Launching"; 
                    this.Refresh();
                    try
                    {
                        String inputPath = (String)((AppShortcut)menu[selected]).getPath();
                        System.Diagnostics.Process launched = System.Diagnostics.Process.Start(@inputPath);

                    }
                    catch (Exception e)
                    {
                        CustomBox.Show("Application not found! \nThis application will no longer be shown in the menu.", "Error!", this.Font, appTree.BackColor, appTree.ForeColor);
                        mu.remove(selected);
                        appTree.Nodes.Remove(appTree.SelectedNode);
                        this.BringToFront();
                        this.Focus();
                    }
                    statusLabel1.Text = "Ready";

                }
            }
        }

        /**
         * Increases or decreases the font size depending on the given parameter.
         * Shows the new font size in the status label.
         */ 
        private void changeFontSize(int change)
        {
            int prevSize = this.Size.Height;
            float fontSize = this.Font.Size;
            fontSize = fontSize + change;
            if (fontSize >= 10)
            {
                this.Font = new Font(this.Font.FontFamily, fontSize, this.Font.Style, this.Font.Unit, this.Font.GdiCharSet, this.Font.GdiVerticalFont);
                statusLabel1.Font = this.Font;
                menuStrip1.Font = this.Font;
                appTreeContextMenu.Font = this.Font;
            }
            if (!tooLarge)
            {
                statusLabel1.Text = "Text Size: " + this.Font.Size;
            }
            else
            {
                statusLabel1.Text = "Text Size: " + this.Font.Size;
            }
            int newSize = this.Size.Height;
            checkScreenSize();

        }

        /**
         * Changes the colour scheme depending on the int received
         */ 
        private void colourComboChanged(int i)
        {
            Color bg = Color.Black;
            Color fg = Color.Black;
            if (i == 0)
            {
                fg = Color.Black;
                bg = Color.White;
            }
            if (i == 1)
            {
                fg = Color.White;
                bg = Color.Black;
            }
            if (i == 2)
            {
                fg = Color.Yellow;
                bg = Color.Navy;
            }
            if (i == 3)
            {
                fg = Color.Black;
                bg = Color.Yellow;
            }
            if (i == 4)
            {
                fg = Color.Black;
                bg = Color.AliceBlue;
            }
            if (i == 5)
            {
                fg = Color.Black;
                bg = Color.Cornsilk;
            }
            if (i == 6)
            {
                fg = Color.Black;
                bg = Color.MistyRose;
            }
            changeForeColour(fg);
            changeBackColour(bg);
        }

        /**
         * Reverses the colour scheme
         */ 
        private void colourSchemeOrderChanged()
        {
            Color tempB = appTree.BackColor;
            Color tempF = appTree.ForeColor;
            appTree.ForeColor = tempB;
            changeBackColour(tempF);
            changeForeColour(tempB);
        }

        /** 
         * Changes the foreground colour of all components
         */
        private void changeForeColour(Color fg)
        {
            appTree.ForeColor = fg;
            menuStrip1.ForeColor = fg;

            downloadMenuItem.ForeColor = fg;
            settingsBg.ForeColor = fg;
            settingsColourScheme.ForeColor = fg;
            settingsDefaultFont.ForeColor = fg;
            settingsFg.ForeColor = fg;
            settingsFont.ForeColor = fg;
            settingsReverse.ForeColor = fg;
            fileMenuExit.ForeColor = fg;
            aboutToolStripMenuItem1.ForeColor = fg;
            keyboardShortcutsToolStripMenuItem.ForeColor = fg;
            fontToolStripMenuItem.ForeColor = fg;
            sizeToolStripMenuItem.ForeColor = fg;
            moreBgMenuItem.ForeColor = fg;
            moreFgMenuItem.ForeColor = fg;
            foreach (ToolStripMenuItem t in settingsBg.DropDownItems)
            {
                if (!t.Text.Equals("Custom..."))
                {
                    t.ForeColor = fg;
                    Color temp = checkClash(t.BackColor, false, false);
                    if (temp != t.BackColor)
                    {
                        t.Enabled = false;
                        t.Visible = false;
                    }
                    else
                    {
                        t.Enabled = true;
                        t.Visible = true;
                    }
                }
            }
            foreach (ToolStripMenuItem t in fontToolStripMenuItem.DropDownItems)
            {
                t.ForeColor = fg;
            }
            foreach (ToolStripMenuItem t in sizeToolStripMenuItem.DropDownItems)
            {
                t.ForeColor = fg;
            }
        }

        /**
         * Changes the background colour of all components
         */
        private void changeBackColour(Color bg)
        {
            appTree.BackColor = bg;
            panel1.BackColor = bg;
            menuStrip1.BackColor = bg;

            downloadMenuItem.BackColor = bg;
            settingsBg.BackColor = bg;
            settingsColourScheme.BackColor = bg;
            settingsDefaultFont.BackColor = bg;
            settingsFg.BackColor = bg;
            settingsFont.BackColor = bg;
            settingsReverse.BackColor = bg;
            fileMenuExit.BackColor = bg;
            aboutToolStripMenuItem1.BackColor = bg;
            keyboardShortcutsToolStripMenuItem.BackColor = bg;
            fontToolStripMenuItem.BackColor = bg;
            sizeToolStripMenuItem.BackColor = bg;
            moreFgMenuItem.BackColor = bg;
            moreBgMenuItem.BackColor = bg;
            foreach (ToolStripMenuItem t in settingsFg.DropDownItems)
            {
                if (!t.Text.Equals("Custom..."))
                {
                    t.BackColor = bg;
                    Color temp = checkClash(t.ForeColor, true, false);
                    if (temp != t.ForeColor)
                    {
                        t.Enabled = false;
                        t.Visible = false;
                    }
                    else
                    {
                        t.Enabled = true;
                        t.Visible = true;
                    }
                }
            }
            foreach (ToolStripMenuItem t in fontToolStripMenuItem.DropDownItems)
            {
                t.BackColor = bg;
            }
            foreach (ToolStripMenuItem t in sizeToolStripMenuItem.DropDownItems)
            {
                t.BackColor = bg;
            }
        }

        /**
         * Calculates contrast ratio between colours to ensure that the text will be readable
         */ 
        public Color checkClash(Color newColor, bool text, bool userCaused)
        {
            if (text) //if it is the text colour that is being changed
            {
                if (newColor.Equals(appTree.BackColor))
                {
                    if (userCaused)
                        CustomBox.Show("You have attempted to change the colour so that the background would be the same as the text. \nThis has been cancelled to avoid problems.", "Warning!", this.Font, this.BackColor, this.ForeColor);
                    return appTree.ForeColor;
                }
                else
                {
                    if (checkRatio(appTree.BackColor, newColor) == true)
                        return newColor;
                    else
                    {
                        if (userCaused)
                            CustomBox.Show("Changing to this colour would give a poor luminosity ratio and would therefore be difficult to read. \nThis has been cancelled to avoid problems.", "Warning!", this.Font, this.BackColor, this.ForeColor);
                        return appTree.ForeColor;
                    }
                }
            }
            else
            {
                if (newColor.Equals(appTree.ForeColor))
                {
                    if (userCaused)
                        CustomBox.Show("You have attempted to change the colour so that the background would be the same as the text. \nThis has been cancelled to avoid problems.", "Warning!", this.Font, this.BackColor, this.ForeColor);
                    return appTree.BackColor;
                }
                {
                    if (checkRatio(newColor, appTree.ForeColor) == true)
                        return newColor;
                    else
                    {
                        if (userCaused)
                            CustomBox.Show("Changing to this colour would give a poor luminosity ratio and would therefore be difficult to read. \nThis has been cancelled to avoid problems.", "Warning!", this.Font, this.BackColor, this.ForeColor);
                        return appTree.BackColor;
                    }
                }
            }
        }

        /**
         * Contrast ratio calculation
         */ 
        public bool checkRatio(Color back, Color fore)
        {
            decimal backR = Decimal.Divide(back.R, 255);
            decimal backG = Decimal.Divide(back.G, 255);
            decimal backB = Decimal.Divide(back.B, 255);
            backR = relLuminance(backR);
            backG = relLuminance(backG);
            backB = relLuminance(backB);

            decimal backVal = (((decimal)0.2126 * backR) + ((decimal)0.7152 * backG) + ((decimal)0.0722 * backB));

            decimal foreR = Decimal.Divide(fore.R, 255);
            decimal foreG = Decimal.Divide(fore.G, 255);
            decimal foreB = Decimal.Divide(fore.B, 255);
            foreR = relLuminance(foreR);
            foreG = relLuminance(foreG);
            foreB = relLuminance(foreB);

            decimal foreVal = (((decimal)0.2126 * foreR) + ((decimal)0.7152 * foreG) + ((decimal)0.0722 * foreB));

            decimal result;
            if (foreVal > backVal)
                result = ((foreVal + (decimal)0.05) / (backVal + (decimal)0.05));
            else result = ((backVal + (decimal)0.05) / (foreVal + (decimal)0.05));

            if (result >= (decimal)4.5)
                return true;
            else return false;

        }

        /**
         * Calculates the relative luminance
         */ 
        public decimal relLuminance(decimal num)
        {
            decimal num2;
            if (num <= (decimal)0.03928)
            {
                num2 = (num / (decimal)12.92);
            }
            else num2 = (decimal)(Math.Pow((double)((num + (decimal)0.055) / (decimal)1.055), 2.4));
            return num2;
        }

        /*
         * Calls fontReset to restore the default font.
         * Changes the colour scheme to black on white.
         * Resets the window size and location.
         */ 
        private void resetAll()
        {
            fontReset();
            changeBackColour(Color.White);
            changeForeColour(Color.Black);
            this.Size = resetSize.Size;
            this.MinimumSize = resetSize.Size;
            this.Location = new Point(0, 0);
            statusLabel1.Text = "Text Size: " + this.Font.Size;
            checkScreenSize();
        }

        /**
         * Resets the font and updates minimum size.
         */ 
        private void fontReset()
        {
            TypeConverter toFont = TypeDescriptor.GetConverter(typeof(Font));
            Font newFont = (Font)toFont.ConvertFromString("Microsoft Sans Serif");
            this.Font = new Font(newFont.FontFamily, float.Parse("10.0"), newFont.Style, newFont.Unit, newFont.GdiCharSet, newFont.GdiVerticalFont);

            statusLabel1.Font = this.Font;
            menuStrip1.Font = this.Font;
            appTreeContextMenu.Font = this.Font;
            this.Size = resetSize.Size;
            this.MinimumSize = resetSize.Size;
            checkScreenSize();

            this.WindowState = FormWindowState.Normal;
            this.Refresh();
        }

        /**
         * Checks the screen size to ensure that the window can not become too big.
         */ 
        private void checkScreenSize()
        {
            maxSize.Width = (Screen.PrimaryScreen.WorkingArea.Width - 18);
            maxSize.Height = Screen.PrimaryScreen.WorkingArea.Height - 18;
            this.MaximumSize = maxSize.Size;
            appTree.Height = this.Height - menuStrip1.Height - (2 * statusStrip1.Height);
            appTree.Scrollable = true;
            if (this.Height == this.MaximumSize.Height || this.Width == this.MaximumSize.Width)
                tooLarge = true;
            else tooLarge = false;
            if (tooLarge)
            {
                trayIcon.BalloonTipText = "The menu window is now larger than your screen. Press the Escape key to fix any layout problems.";
                trayIcon.ShowBalloonTip(250);
            }
        }

        /**
         * General key commands that are also called whenever the KeyDown occurs on the other Form components
         */
        private void MenuForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F && e.Control)
                settingsFont.ShowDropDown();
            if (e.KeyCode == Keys.B && e.Control)
                settingsBg.ShowDropDown();
            if (e.KeyCode == Keys.T && e.Control)
                settingsFg.ShowDropDown();
            if (e.KeyCode == Keys.D && e.Control)
                fontReset();
            if (e.KeyCode == Keys.R && e.Control)
                colourSchemeOrderChanged();
            if (e.KeyCode == Keys.D1 && e.Control)
                colourComboChanged(0);
            if (e.KeyCode == Keys.D2 && e.Control)
                colourComboChanged(1);
            if (e.KeyCode == Keys.D3 && e.Control)
                colourComboChanged(2);
            if (e.KeyCode == Keys.D4 && e.Control)
                colourComboChanged(3);
            if (e.KeyCode == Keys.D5 && e.Control)
                colourComboChanged(4);
            if (e.KeyCode == Keys.D6 && e.Control)
                colourComboChanged(5);
            if (e.KeyCode == Keys.D7 && e.Control)
                colourComboChanged(6);
            if (e.KeyCode == Keys.Oemplus && e.Control)
                changeFontSize(2);
            if (e.KeyCode == Keys.OemMinus && e.Control)
                changeFontSize(-2);
            if (e.KeyCode == Keys.Escape)
                resetAll();
            if (e.KeyCode == Keys.Alt)
                menuStrip1.Focus();
            if (e.KeyCode == Keys.F1)
                System.Diagnostics.Process.Start("Help.txt");
        }

        /**
         * Saves settings and closes
         */ 
        private void MenuForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveAndClose();
        }

        /**
         * Checks if the window has been minimized and if so hides it in the system tray and shows a balloon tip
         */
        private void MenuForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.BalloonTipText = "Your application menu is still running and can be accessed through this icon";
                trayIcon.ShowBalloonTip(200);
            }
        }

        /**
         * Selects the top node when tabbing into the appTree
         */ 
        private void appTree_Enter(object sender, EventArgs e)
        {
            try
            {
                appTree.SelectedNode = appTree.Nodes[0];
            }
            catch
            {

            }
        }

        /**
        * Displays information message when a single click is made on the appTree
        */
        private void appTree_Click(object sender, EventArgs e)
        {
            if (appTree.SelectedNode != (null))
            {
                statusLabel1.Text = "Double click an app to launch";
            }

        }

        /**
         * Launch the application that is double-clicked
         */
        private void appTree_DoubleClick(object sender, EventArgs e)
        {
            launchApp();
        }    

        /**
         * Keyboard listener for when the appTree is in focus.
         * If Enter key is pressed, then call the launchApp method
         */
        private void appTree_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyData == Keys.Enter)
            {
                launchApp();
            }
            if (e.KeyData == Keys.Up)
            {
                if (appTree.SelectedNode != (null))
                {
                    statusLabel1.Text = "Press Enter to launch";

                    appTree.SelectedNode.EnsureVisible();
                }
            }
            if (e.KeyData == Keys.Down)
            {
                if (appTree.SelectedNode != (null))
                {
                    statusLabel1.Text = "Press Enter to launch";

                    appTree.SelectedNode.EnsureVisible();
                }
            }
            else MenuForm_KeyDown(sender, e);
        }         

        /**
         * Save settings and close
         */ 
        private void fileMenuExit_Click(object sender, EventArgs e)
        {
            saveAndClose();
        }

        /**
         * Provides information on downloading additional applications
         */ 
        private void downloadMenuItem_Click(object sender, EventArgs e)
        {
            CustomBox.Show("Coming Soon! \nThis feature will allow new applications to be downloaded and added to the menu automatically. \nPlease visit http://access.ecs.soton.ac.uk to view progress on this feature. \nThis website will be launched when you close this message.", "Download Information", this.Font, appTree.BackColor, appTree.ForeColor);
            System.Diagnostics.Process.Start("http://access.ecs.soton.ac.uk");
            this.BringToFront();
            this.Focus();       
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void blackOnWhiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(0);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void whiteOnBlackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(1);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void yellowOnBlueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(2);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void blackOnYellowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(3);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void blackOnPaleBlueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(4);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void blackOnCreamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(5);
        }

        /**
         * Changes to selected colour scheme
         */ 
        private void blackOnPinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            colourComboChanged(6);
        }

        /**
         * Reverses colour scheme
         */ 
        private void settingsReverse_Click(object sender, EventArgs e)
        {
            colourSchemeOrderChanged();
        }

        /**
         * Changes background colour to the selected colour
         */ 
        private void settingsBg_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!e.ClickedItem.Text.Equals("Custom..."))
            {
                Color bg = appTree.BackColor;
                bg = checkClash(e.ClickedItem.BackColor, false, false);
                changeBackColour(bg);
            }
        }

        /**
         * Changes foreground colour to the selected colour
         */
        private void settingsFg_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!e.ClickedItem.Text.Equals("Custom..."))
            {
                Color fg = appTree.ForeColor;
                fg = checkClash(e.ClickedItem.ForeColor, true, false);
                changeForeColour(fg);
            }
        }

        /**
         * Changes background colour to a custom colour
         */ 
        private void moreBgMenuItem_Click(object sender, EventArgs e)
        {
            Color toReturn = appTree.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                toReturn = checkClash(colorDialog1.Color, false, true);
            }
            changeBackColour(toReturn);
        }

        /**
         * Changes foreground colour to a custom colour
         */ 
        private void moreFgMenuItem_Click(object sender, EventArgs e)
        {
            Color toReturn = appTree.ForeColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                toReturn = checkClash(colorDialog1.Color, true, true);
            }
            changeForeColour(toReturn);
        }

        /**
         * Changes font type to the selected font
         */ 
        private void fontToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            TypeConverter toFont = TypeDescriptor.GetConverter(typeof(Font));
            String selected = e.ClickedItem.Text;
            Font newFont = (Font)toFont.ConvertFromString(selected);
            this.Font = new Font(newFont.FontFamily, this.Font.Size, newFont.Style, newFont.Unit, newFont.GdiCharSet, newFont.GdiVerticalFont);
            statusStrip1.Font = this.Font;
            menuStrip1.Font = this.Font;
        }

        /**
         * Changes font size to the selected size
         */
        private void sizeToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            int selected = int.Parse(e.ClickedItem.Text);
            TypeConverter toFont = TypeDescriptor.GetConverter(typeof(Font)); 
            Font newFont = appTree.Font;
            this.Font = new Font(newFont.FontFamily, selected, newFont.Style, newFont.Unit, newFont.GdiCharSet, newFont.GdiVerticalFont);

            statusLabel1.Font = this.Font;
            menuStrip1.Font = this.Font;
            appTreeContextMenu.Font = this.Font;
            statusLabel1.Text = "";
            checkScreenSize();
        }

        /**
         * Resets the font
         */ 
        private void settingsDefaultFont_Click(object sender, EventArgs e)
        {
            fontReset();
        }

        /**
         * Shows the keyboard shortcuts in a popup window.
         */ 
        private void keyboardShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            String shortcuts = "";
            Char[] separator = ",".ToCharArray();
            foreach (String shortcut in shortcutKeys)
            {
                String[] subs = shortcut.Split(separator);
                shortcuts += subs[0] + " : " + subs[1] + ". \n";
            }
            CustomBox.Show(shortcuts, "Keyboard Shortcuts", this.Font, appTree.BackColor, appTree.ForeColor);
            this.BringToFront();
            this.Focus();
        }

        /**
         * Shows the about window
         * For new versions, change the versionCreatedBy and versionContactAddress to show in the about box.
         */ 
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            double version = 0.3;
            String versionCreatedBy = "Chris Phethean";
            String versionContactAddress = "http://users.ecs.soton.ac.uk/cjp106";
            CustomBox.Show("Menu \nVersion " + version + "\nVersion created by: " + versionCreatedBy + "\n" + versionContactAddress + " \n\nhttp://access.ecs.soton.ac.uk/#0 \nECS Accessibility Projects, \nLearning Societies Lab, \nSchool of Electronics and Computer Science, \nUniversity of Southampton. \nFunded by LATEU. \nContact: Dr Mike Wald: http://www.ecs.soton.ac.uk/people/mw ", "Access Tools - About", this.Font, appTree.BackColor, appTree.ForeColor);
            this.BringToFront();
            this.Focus();
        }     

        /**
         * Restores the window if the user doubleclicks on the icon in the system tray
         */ 
        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        /** 
         * Restores the window if the user clicks on the notification balloon that is shown when the window minimizes
         */
        private void trayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        /**
         * Restores the window if the user Right-clicks on the system tray icon and selects Show
         */ 
        private void itemShow_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        /**
         * Exits the menu completely if the user Right-clicks on the system tray icon and selects Exit
         */ 
        private void itemExit_Click(object sender, EventArgs e)
        {
            saveAndClose();
        }

        /**
         * Gets the path of the selected application and launches it
         */ 
        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            String selected = e.ClickedItem.Text; 
            if (!(selected.Equals("Show Menu") || selected.Equals("Exit Menu")))
            {
                String inputPath = (String)((AppShortcut)menu[selected]).getPath();
                try
                {
                    System.Diagnostics.Process launched = System.Diagnostics.Process.Start(@inputPath);
                }
                catch (Exception ex)
                {
                CustomBox.Show("Application not found! \nThis application will not be shown when the menu is next loaded", "Error!", this.Font, appTree.BackColor, appTree.ForeColor);
                mu.remove(selected);
                this.BringToFront();
                this.Focus();
                }
            }

        }

        private void appTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                appTree.SelectedNode = e.Node;
            }

        }

        private void launchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            launchApp();
        }

        private void descriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
             
            if (appTree.SelectedNode != (null))
            {
                String selected = appTree.SelectedNode.Text;
                char[] extraSplit = "(".ToCharArray();
                try
                {
                    selected = selected.Substring(0, selected.LastIndexOfAny(extraSplit) - 1);
                }
                catch
                {

                }
                String current = appTree.SelectedNode.Text.Substring(appTree.SelectedNode.Text.LastIndexOfAny(extraSplit) + 1);
                current = current.Substring(0, current.Length - 1);
                String description = CustomBox.Show(current, "Edit Description - " + selected, this.Font, this.BackColor, this.ForeColor);

                try
                {
                    mu.editExtra(selected, description);
                    ((AppShortcut)menu[selected]).setExtra(description);
                    String extra = (String)((AppShortcut)menu[selected]).getExtra();
                    TreeNode toChange = appTree.SelectedNode;
                    appTree.BeginUpdate();
                    if (extra.Equals(""))
                    {
                        toChange.Text = selected;
                    }
                    else
                    {
                        toChange.Text = selected + " (" + extra + ")";
                    }
                    appTree.EndUpdate();
                    appTree.Refresh();                    
                }
                catch
                {
                    CustomBox.Show("Could not update description", "Error", this.Font, this.BackColor, this.ForeColor);
                }
            }
        }

        private void hToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CustomBox.Show("This applicaiton will not be shown on the menu until it is reloaded. \n(This application has not been removed from your pendrive.)", "Access Tools", this.Font, appTree.BackColor, appTree.ForeColor);
            appTree.Nodes.Remove(appTree.SelectedNode);
            
        }

    }
}
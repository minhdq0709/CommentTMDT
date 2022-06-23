using CefSharp;
using CefSharp.WinForms;
using CommentTMDT.Controller;
using CommentTMDT.Enum;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommentTMDT
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser _browser = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /* Load data on combo box */
            cbNamePage.DisplayMember = "Text";
            cbNamePage.ValueMember = "Value";

            var items = new[] {
                new { Text = "Bách hóa xanh", Value = NamePage.BACH_HOA_XANH },
                new { Text = "Bạch Long", Value = NamePage.BACH_LONG },
                new { Text = "Cell phoneS", Value = NamePage.CELL_PHONE_S },
                new { Text = "Chợ lớn", Value = NamePage.CHO_LON },
                new { Text = "DDTM", Value = NamePage.DI_DONG_THONG_MINH },
                new { Text = "FPT shop", Value = NamePage.FPT_SHOP },
                new { Text = "Hoàng hà", Value = NamePage.HOANG_HA },
                new { Text = "Kid plaza", Value = NamePage.KID_PLAZA },
                new { Text = "Lazada", Value = NamePage.LAZADA },
                new { Text = "Meta", Value = NamePage.META },
                new { Text = "Sendo", Value = NamePage.SENDO },
                new { Text = "Shoppe", Value = NamePage.SHOPEE },
                new { Text = "Tiki", Value = NamePage.TIKI },
                new { Text = "Viette store", Value = NamePage.VIETTEL_STORE }
            };

            cbNamePage.DataSource = items;
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            NamePage a = (NamePage)cbNamePage.SelectedValue;
            switch (a)
            {
                case NamePage.BACH_HOA_XANH:
                    BachHoaXanh bhx = new BachHoaXanh();
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await bhx.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = "Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.BACH_LONG:
                    InitBrowser("https://bachlongmobile.com");
                    await Task.Delay(20_000);
                    BachLong bl = new BachLong(_browser);

                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await bl.CrawlData();
                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }
                case NamePage.CELL_PHONE_S:
                    while (true)
                    {
                        try
                        {
                            InitBrowser("https://cellphones.com.vn/");
                            await Task.Delay(20_000);

                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            CellPhoneS cs = new CellPhoneS(_browser, lbSumComment, lbIsError);
                            await cs.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromHours(1));
                    }
                case NamePage.CHO_LON:
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            ChoLon cl = new ChoLon(lbSumComment, lbIsError);
                            await cl.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromHours(1));
                    }
                case NamePage.DI_DONG_THONG_MINH:
                    DiDongThongMinh dd = new DiDongThongMinh();
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await dd.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.FPT_SHOP:
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            InitBrowser("https://fptshop.com.vn/");
                            await Task.Delay(20_000);

                            FptShop fpt = new FptShop(_browser, lbIsError, lbSumComment);
                            await fpt.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromHours(1));
                    }
                case NamePage.HOANG_HA:
                    HoangHa hh = new HoangHa();
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await hh.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.KID_PLAZA:
                    InitBrowser("https://www.kidsplaza.vn/");
                    await Task.Delay(20_000);

                    Kidplaza kd = new Kidplaza(_browser);

                    while (true)
                    {
                        try
                        {
                            

                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";
                            
                            await kd.CrawlData();

                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = $"Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.LAZADA:
                    while (true)
                    {
                        Lazada lz = new Lazada(lbIsError, lbSumComment);
                        await lz.CrawlData();
                    }
                case NamePage.META:
                    Meta mt = new Meta();
                    while(true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await mt.CrawData();
                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = "Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.SENDO:
                    Sendo sd = new Sendo();
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await sd.CrawData();
                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception)
                        {
                            lbIsError.Text = "Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                case NamePage.SHOPEE:
                    break;
                case NamePage.TIKI:
                    Tiki tk = new Tiki();
                    while (true)
                    {
                        try
                        {
                            lbStatus.Text = "Đang chạy";
                            lbIsError.Text = "Ko lỗi";

                            await tk.CrawlData();
                            lbStatus.Text = $"Đang dừng {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}";
                        }
                        catch (Exception) {
                            lbIsError.Text = "Có lỗi";
                        }

                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
            }
        }

        public void InitBrowser(string urlBase)
        {
            if (_browser == null)
            {
                this.WindowState = FormWindowState.Maximized;
                CefSettings s = new CefSettings();

                Cef.Initialize(s);
                _browser = new ChromiumWebBrowser(urlBase);
                this.panel1.Controls.Add(_browser);
                _browser.Dock = DockStyle.Fill;
            }
        }
    }
}

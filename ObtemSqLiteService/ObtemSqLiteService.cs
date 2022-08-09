using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ObtemSqLiteService
{
    public partial class ObtemSqLiteService : ServiceBase
    {
        public string ArquivoGeradoSqLiteServidor { get; set; }
        public string HoraInicial { get; set; }
        private Timer tmrCopiaSqLite { get; set; }
        private BackgroundWorker bgwCopiaSqLite { get; set; }
        public ObtemSqLiteService()
        {
            InitializeComponent();

            ArquivoGeradoSqLiteServidor = ConfigurationManager.AppSettings["ARQUIVO_GERADO_SQLITE"].ToString();
            HoraInicial = ConfigurationManager.AppSettings["HORA_INICIAL"].ToString();

            tmrCopiaSqLite = new Timer();
            bgwCopiaSqLite = new BackgroundWorker();
        }

        protected override void OnStart(string[] args)
        {
            tmrCopiaSqLite.Interval = 5000;
            tmrCopiaSqLite.Elapsed += TmrCopiaSqLite_Elapsed;

            bgwCopiaSqLite.DoWork += BgwCopiaSqLite_DoWork;
            bgwCopiaSqLite.RunWorkerCompleted += BgwCopiaSqLite_RunWorkerCompleted;

            tmrCopiaSqLite.Enabled = true;
        }

        protected override void OnStop()
        {
        }

        private void TmrCopiaSqLite_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                DateTime inicial = Convert.ToDateTime($"{DateTime.Now.ToString("dd/MM/yyyy")} {HoraInicial}");

                if (DateTime.Now > inicial && !bgwCopiaSqLite.IsBusy)
                {
                    bgwCopiaSqLite.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                LogErro(ex.Message);
            }
        }

        private void BgwCopiaSqLite_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                CopiarSqLite();
            }
            catch (Exception ex)
            {
                LogErro(ex.Message);
            }
        }

        private void BgwCopiaSqLite_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogErro(e.Error.Message);
            }

            GC.Collect();
        }

        public void CopiarSqLite()
        {
            try
            {
                tmrCopiaSqLite.Enabled = false;
                string arquivoGeradoServidor = ArquivoGeradoSqLiteServidor + "\\NOSSADRGLOJAS.db";
                string arquivoClienteZip = @"C:\SqLite\NOSSADRGLOJAS.zip";

                FileInfo arquivoSqLiteServidor = new FileInfo(arquivoGeradoServidor);
                FileInfo arqZip = new FileInfo(arquivoClienteZip);

                if (arqZip.Exists)
                {
                    if (arquivoSqLiteServidor.LastWriteTime > arqZip.LastWriteTime)
                    {
                        //File.Copy(arquivoGerado, diretorioDestino + Path.GetFileName(arquivoGerado), true);
                        arqZip.Delete();
                        ZipFile.CreateFromDirectory(ArquivoGeradoSqLiteServidor, arquivoClienteZip);
                    }
                }
                else
                {
                    //File.Copy(arquivoGerado, diretorioDestino + Path.GetFileName(arquivoGerado));
                    ZipFile.CreateFromDirectory(ArquivoGeradoSqLiteServidor, arquivoClienteZip);
                }

                tmrCopiaSqLite.Enabled = true;
            }
            catch (Exception ex)
            {
                tmrCopiaSqLite.Enabled = true;
                LogErro(ex.Message);
            }
        }

        private void LogErro(string msgErro)
        {
            try
            {
                string caminhoArquivo = string.Format("{0}\\LogErro-{1:yyyy-MM-dd}.txt", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), DateTime.Now);
                File.AppendAllText(caminhoArquivo, msgErro);
                File.AppendAllText(caminhoArquivo, Environment.NewLine);
            }
            catch (System.Exception)
            {
            }
        }
    }
}

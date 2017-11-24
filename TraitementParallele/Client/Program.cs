using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Configuration;

namespace Client
{
	class Program
	{
		/// <summary>
		/// Générateur de client/demande de traitement au serveur.
		/// Dans un 1er temps, demande des infos de connexion au serveur à l'utilisateur.
		/// Ici un ThreadPool est utilisé pour bloquer le nombre de traitements simultanés à 20 maximum.
		/// Une boucle infinie temporisée se charge de générer des demandes aléatoires.
		/// Chaque demande est envoyée au serveur par un thread de communication client TCP,
		/// qui affichera la réponse du serveur une fois le traitement terminé.
		/// </summary>
		static void Main(string[] args)
		{
			Console.WriteLine("[Générateur]");
			string ip = ConfigurationManager.AppSettings["ip"];
			while (ip == "")
			{
				Console.Write("Entrez l'adresse du serveur de traitement : ");
				ip = Console.ReadLine();
			}

			int numPort = -1;
			int.TryParse(ConfigurationManager.AppSettings["port"], out numPort);
			string rep = null;
			while (numPort == -1)
			{
				Console.Write("Entrez le port du serveur de traitement : ");
				rep = Console.ReadLine();
				int.TryParse(rep, out numPort);
			}
			// Période d'émission des demandes (en ms)
			int p = 5000;
			int.TryParse(ConfigurationManager.AppSettings["period"], out p);

			Console.WriteLine("ip = {0}, numPort = {1}, period = {2}", ip, numPort, p);

			// Blocage du nombre de demandes simultanées maximum
			ThreadPool.SetMaxThreads(20, 20);
			Console.WriteLine("Ctrl+C pour stopper le générateur...");
			int nbDemande = 1;
			while (true)
			{
				ThreadPool.QueueUserWorkItem(DemandeTraitement,
											new Client() {	Ip = ip,
															Port = numPort,
															T = new Traitement(nbDemande) });
				nbDemande++;
				Thread.Sleep(p); // Temporisation pour visualisation
			}
		}

		/// <summary>
		/// Méthode de démarrage d'un thread fils, c'est ici qu'il se
		/// connectera et enverra sa demande de traitement au serveur.
		/// </summary>
		/// <param name="data"> Les données passer au thread client, il est attendu un Client.Client </param>
		public static void DemandeTraitement(Object data)
		{
			Client cli = data as Client;
			Console.WriteLine("{0} : Début de la demande {1}", DateTime.Now.ToString("HH:mm:ss"), cli.T.No);
			try
			{
				TcpClient tcpCli = new TcpClient(cli.Ip, cli.Port);
				using (tcpCli)
				{
					NetworkStream networkStream = tcpCli.GetStream();
					using (networkStream)
					{
						StreamReader reader = new StreamReader(networkStream);
						using (reader)
						{
							StreamWriter writer = new StreamWriter(networkStream);
							using (writer)
							{
								writer.AutoFlush = true;

								// Sérialisation de la demande
								XmlSerializer s = new XmlSerializer(typeof(Traitement));
								MemoryStream mems = new System.IO.MemoryStream();
								s.Serialize(mems, cli.T);

								// Envoi de la demande
								string str = Encoding.UTF8.GetString(mems.ToArray());
								writer.WriteLine(str);
								writer.WriteLine(); // envoi d'une ligne vide pour signaler la fin des données
								Console.WriteLine("{0} : Demande {1} envoyée", DateTime.Now.ToString("HH:mm:ss"), cli.T.No);

								// mise en attente de la réponse du serveur
								string rep = reader.ReadLine();
								Console.WriteLine("{0} : Demande {1} a recu : {2} (FIN)", DateTime.Now.ToString("HH:mm:ss"), cli.T.No, rep);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("{0} : La demande {1} a rencontré une erreur", DateTime.Now.ToString("HH:mm:ss"), cli.T.No);
				Console.Error.WriteLine("Message : {0}", e);
			}

		}
	}

	// Classe utilitaire pour le passage de données entre thread
	class Client {
		public string Ip { get; set; }
		public int Port { get; set; }
		public Traitement T { get; set; }
	}

}

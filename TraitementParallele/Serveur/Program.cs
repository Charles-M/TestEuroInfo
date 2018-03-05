using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using Client;
using System.Configuration;

namespace Server
{
	class Program
	{
		// Les ressources critiques
		// Nombre de threads en cours de traitement
		static int nbThread = 0;

		// Nombre de traitements accomplis par priorité
		// Les priorités sont dans l'ordre de Thread.Priority : [4 2 5 1 3] (5 = la priorité la plus haute)
		static int[] nbTacheTraitee = new int[] { 0, 0, 0, 0, 0 };

		static Mutex m = new Mutex();

		/// <summary>
		/// Serveur de traitement.
		/// Dans un 1er temps le serveur demande un numéro de port.
		/// Le socket Tpc d'écoute est ensuite ouvert dans l'attente de travaux dans une boucle sans fin.
		/// Pour chaque client, une demande sérialisée est reçue et traitée par un thread fils.
		/// </summary>
		static void Main(string[] args)
		{
			Console.WriteLine("[Server]");
			int numPort = -1;
			int.TryParse(ConfigurationManager.AppSettings["port"], out numPort);
			string rep = null;
			while (numPort == -1)
			{
				Console.Write("Entrez le port du serveur de traitement : ");
				rep = Console.ReadLine();
				int.TryParse(rep, out numPort);
			}

			TcpListener tcpList = new TcpListener(IPAddress.Any, numPort);
			try {
				tcpList.Start();
			} catch (Exception e) {
				Console.Error.WriteLine("Le serveur a rencontré un problème d'initialisation...");
				Console.Error.WriteLine("Message : {0}", e);
			}

			Console.WriteLine("Serveur lancé : {0}", tcpList.LocalEndpoint);
			Console.WriteLine("Ctrl+C pour stopper le serveur...");
			TcpClient tcpCli = null;
			while (true) {
				try {
					tcpCli = tcpList.AcceptTcpClient();

					NetworkStream networkStream = tcpCli.GetStream();
					StreamReader reader = new StreamReader(networkStream);
					StreamWriter writer = new StreamWriter(networkStream);
					writer.AutoFlush = true;

					// Lecture du buffer jusqu'à la ligne vide
					StringBuilder dataRead = new StringBuilder();
					string line = reader.ReadLine();
					while (line != "")
					{
						dataRead.Append(line);
						line = reader.ReadLine();
					}

					// Dé-sérialisation de la demande
					XmlSerializer serializer = new XmlSerializer(typeof(Traitement));
					XmlReader xr = XmlReader.Create(new StringReader(dataRead.ToString()));
					Traitement demande = null;
					if (serializer.CanDeserialize(xr))
						demande = (Traitement)serializer.Deserialize(xr);

					// Création d'un thread pour traiter la demande
					Console.WriteLine("{0} : Réception d'une demande {1} ", DateTime.Now.ToString("HH:mm:ss"), demande);
					 Thread t = new Thread(ExecuteTraitement);
					t.Priority = demande.Priority;
					t.Start(new Client() { Demande = demande, TcpCli = tcpCli });

					m.WaitOne();
						// mise en valeur du nombre max de thread = 20
						Console.WriteLine("\nNombre de threads concurrents : {0}", nbThread);

						// mise en valeur de l'ordonnancement
						Console.Write("Nombre de tâches traitées par priorité [4 2 5 1 3] : [");
						foreach (int val  in nbTacheTraitee)
							Console.Write(val+"  ");
						Console.WriteLine("]\n");
					m.ReleaseMutex();

				} catch (Exception e) {
					Console.Error.WriteLine("Le serveur a rencontré un problème à la réception d'une demande...");
					Console.Error.WriteLine("Message : {0}", e);
				}
			}
			
		}

		/// <summary>
		/// Méthode de démarrage d'un thread de traitement qui se chargera
		/// de répondre au client une fois le calcul terminé.
		/// Il met aussi à jour les sections critiques.
		/// </summary>
		/// <param name="data"> Les données nécéssaires au traitement de la demande, type attendu : Server.Client </param>
		public static void ExecuteTraitement(Object data) {
			Client cli = data as Client;
			TcpClient tcpCli = cli.TcpCli;
			try {
				using (cli.TcpCli)
				{
					using (tcpCli.GetStream())
					{
						StreamReader reader = new StreamReader(tcpCli.GetStream());
						using (reader)
						{
							StreamWriter writer = new StreamWriter(tcpCli.GetStream());
							using (writer)
							{
								// Section critique
								m.WaitOne();
									nbThread++;
								m.ReleaseMutex();

								Console.WriteLine("{0} : Traitement de la demande {1}", DateTime.Now.ToString("HH:mm:ss"), cli.Demande);

								// Faire un traitement en fonction du Type...
								Thread.Sleep(cli.Demande.Duree * 1000);
								Console.WriteLine("{0} : Traitement de la demande {1} terminé", DateTime.Now.ToString("HH:mm:ss"), cli.Demande);
								writer.WriteLine("Ok");

								// Section critique
								m.WaitOne();
									nbThread--;
									nbTacheTraitee[(int)cli.Demande.Priority]++;
								m.ReleaseMutex();
							}
						}
					}
				}
				
			} catch (Exception e) {
				Console.Error.WriteLine("Le serveur a rencontré une erreur dans le traitement de la demande {0}...", cli.Demande);
				Console.Error.WriteLine("Message : {0}", e);
			}
		}
	}

	// Classe pour le passage de données entre thread
	class Client {
		public TcpClient TcpCli { get; set; }
		public Traitement Demande { get; set; }
	}
}

using System;
using System.Threading;

namespace Client
{
	/// <summary>
	/// Les différents type de traitement qui peuvent être demandé au serveur.
	/// </summary>
	public enum TypeTraitement { T1, T2, T3, T4 }

	/// <summary>
	/// Classe de représentation d'une demande de traitement.
	/// Celle ci est munie d'un type de traitement, d'une priorité d'execution et d'une durée pour simuler le traitement
	/// Son numéro permet de l'identifier sur le réseau
	/// </summary>
	[Serializable]
	public class Traitement
	{
		static Random rand = new Random(DateTime.Now.Millisecond);
		public TypeTraitement Type { get; set; }
		public ThreadPriority Priority { get; set; }
		public int Duree { get; set; }
		public int No { get; set; }

		public Traitement()
		{ }

		// Ce constructeur donne des valeurs aleatoires pour Type, Priotity et Durée
		public Traitement(int num)
		{
			Array typeValues = Enum.GetValues(typeof(TypeTraitement));
			Type = (TypeTraitement)typeValues.GetValue(rand.Next(typeValues.Length));

			Array priorityValues = Enum.GetValues(typeof(ThreadPriority));
			Priority = (ThreadPriority)priorityValues.GetValue(rand.Next(priorityValues.Length));

			Duree = rand.Next(1, 11);
			No = num;
		}

		public Traitement(TypeTraitement t, ThreadPriority p, int d)
		{
			Type = t;
			Priority = p;
			Duree = d;
		}

		public override string ToString()
		{
			return string.Format("Demande[No={3}]( {0}, {1}, {2}s)", Type, Priority, Duree, No);
		}
	}
}

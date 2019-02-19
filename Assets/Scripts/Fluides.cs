using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluides : MonoBehaviour {

    public GameObject m_Particule;
    //lieu de spawn
    public Transform m_Spawn;
    //timer de spawn
    private float timer = 0f;
    //compteur de particul
    private int compteur = 0;
    //nombre de particules désiré
    private int nombreParticule = 100;
    //liste des particules
    public List<GameObject> ParticleList = new List<GameObject>();


    //constante gravitationnelle
    private float g = 9.81f;
    //vecteur vers le bas
    private Vector3 bas = new Vector3(0f, -1f);
    //coef d'elasticité
    private float alpha = 1000f;
    
    //pour le Hash
    private int largeur = 10;
    //le dico
    Dictionary<int, List<GameObject>> Buckets = new Dictionary<int, List<GameObject>>();


    //variable arbitraire pour le rayon de prise en compte des particules voisines
    public float h = 1f;
    //densité voulue
    public float densiteZero = 3f;
    // k ?
    public float k = 3f;
    public float kNear = 5f;
    
    // Use this for initialization
    void Start () {
        
    }
    
    void FixedUpdate()
    {
        timer += Time.deltaTime;
        if (compteur < nombreParticule)
        {
            CreateParticle();
            timer = 0;
            compteur++;
        }
        CreateBucket();

        ApplyGravity();
        SavePrevPosition();

        //calcul densité avec "opti"
        //CalculateDensity();
        //calcul densité sans opti
        CalculateDensity2();

        Collision();
        //ClearBucket();
        NewVelocity();
    }

    //création d'une particule
    void CreateParticle()
    {
        GameObject particule = Instantiate(m_Particule, m_Spawn.position, m_Spawn.rotation);
        particule.GetComponent<Movement>().m_vitesse = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
        ParticleList.Add(particule);
    }

    //application de la gravité
    void ApplyGravity()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            ParticleList[i].GetComponent<Movement>().m_vitesse += bas * g * Time.fixedDeltaTime;
        }
    }

    //sauvegarde de la position et déplacement de la particule
    void SavePrevPosition()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            ParticleList[i].GetComponent<Movement>().m_prev = ParticleList[i].transform.position;
            ParticleList[i].transform.position += ParticleList[i].GetComponent<Movement>().m_vitesse * Time.fixedDeltaTime;
        }
    }

    //fonction double density relaxation avec opti
    void CalculateDensity()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            float densiteNear = 0;
            float Density = 0;

            List<GameObject> voisins = GetNearBy(ParticleList[i]);

            for (int j = 0; j < voisins.Count; j++)
            {
                float distance = Vector3.Distance(ParticleList[i].transform.position, voisins[j].transform.position);
                Density += Mathf.Pow((1 - distance / h), 2);
                densiteNear += Mathf.Pow((1 - distance / h), 3);
            }

            float pression = k * (Density - densiteZero);
            float pNear = kNear * densiteNear;
            Vector3 DX = Vector3.zero;



            for (int j = 0; j < voisins.Count; j++)
            {
                float distance = Vector3.Distance(ParticleList[i].transform.position, voisins[j].transform.position);
                //application des forces
                Vector3 Rij = (voisins[j].transform.position - ParticleList[i].transform.position).normalized;
                float p = pression;
                float q = distance / h;
                float deltaTcarre = Mathf.Pow(Time.fixedDeltaTime, 2);

                Vector3 D = deltaTcarre * (p * (1 - q) + pNear * Mathf.Pow((1 - q), 2)) * Rij;
                voisins[j].transform.position += D / 2;
                DX = -D / 2;
            }
            ParticleList[i].transform.position += DX;
        }
    }

    //fonction double density relaxation sans opti
    void CalculateDensity2()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            float densiteNear = 0;
            float Density = 0;

            for (int j = 0; j < ParticleList.Count; j++)
            {
                if (ParticleList[i] == ParticleList[j])
                {
                    continue;
                }
                float distance = Vector3.Distance(ParticleList[i].transform.position, ParticleList[j].transform.position);
                if (distance < h)
                {
                    Density += Mathf.Pow((1 - distance / h), 2);
                    densiteNear += Mathf.Pow((1 - distance / h), 3);
                }
            }

            float pression = k * (Density - densiteZero);
            float pressionNear = kNear * densiteNear;
            Vector3 DX = Vector3.zero;



            for (int j = 0; j < ParticleList.Count; j++)
            {
                if (ParticleList[i] == ParticleList[j])
                {
                    continue;
                }
                float distance = Vector3.Distance(ParticleList[i].transform.position, ParticleList[j].transform.position);
                if (distance < h)
                {
                    //application des forces
                    Vector3 Rij = (ParticleList[j].transform.position - ParticleList[i].transform.position).normalized;
                    float p = pression;
                    float q = distance / h;
                    float deltaTcarre = Mathf.Pow(Time.fixedDeltaTime, 2);

                    Vector3 D = deltaTcarre * (p * (1 - q) + pressionNear * Mathf.Pow((1 - q), 2)) * Rij;
                    ParticleList[j].transform.position += D / 2;
                    DX = -D / 2;
                }
            }
            ParticleList[i].transform.position += DX;
        }
    }




    //gestion des collisions
    void Collision()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            if (ParticleList[i].transform.position.y < -5)
            {
                float distanceInWall = -5 - ParticleList[i].transform.position.y;
                ParticleList[i].transform.position += new Vector3(0, distanceInWall * alpha * (Time.fixedDeltaTime) * Time.fixedDeltaTime, 0);
            }
            if (ParticleList[i].transform.position.x < -5)
            {
                float distanceInWall = -5 - ParticleList[i].transform.position.x;
                ParticleList[i].transform.position += new Vector3(distanceInWall * alpha * (Time.fixedDeltaTime) * Time.fixedDeltaTime, 0, 0);
            }
            if (ParticleList[i].transform.position.x > 5)
            {
                float distanceInWall = ParticleList[i].transform.position.x - 5;
                ParticleList[i].transform.position += new Vector3(-distanceInWall * alpha * (Time.fixedDeltaTime) * Time.fixedDeltaTime, 0, 0);
            }
        }
    }
    
    //calcul la nouvelle vitesse
    void NewVelocity()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            ParticleList[i].GetComponent<Movement>().m_vitesse = (ParticleList[i].transform.position - ParticleList[i].GetComponent<Movement>().m_prev) / Time.fixedDeltaTime;
        }
    }



    //création de  tous mes buckets
    void CreateBucket()
    {
        for (int i = 0; i < ParticleList.Count; i++)
        {
            AddParticuleToBucket(ParticleList[i]);
        }
    }

    //vide le Bucket
    void ClearBucket()
    {
        Buckets.Clear();
    }

    //Ajoute le GO dans le dictionnaire à la position approprié
    void AddParticuleToBucket(GameObject obj)
    {
        int cellId = GetIdForGO(obj);
        if (!Buckets.ContainsKey(cellId))
        {
            Buckets[cellId] = new List<GameObject>();
        }
        Buckets[cellId].Add(obj);
    }
    
    //détermine l'ID du GO en fonction de sa position
    int GetIdForGO(GameObject obj)
    {
        int posX = Mathf.FloorToInt(obj.transform.position.x);
        int posY = Mathf.FloorToInt(obj.transform.position.y);
        int hashID = (posX + posY)*10;
        //int hashID = posX + posY * largeur;
        return hashID;
    }

    //récupere la liste des particules voisines
    List<GameObject> GetNearBy (GameObject obj)
    {
        List<GameObject> objects = new List<GameObject>();
        int objID = GetIdForGO(obj);
        if (Buckets.ContainsKey(objID))
        {
            objects = Buckets[objID];
        }
        return objects;
    }
    
}

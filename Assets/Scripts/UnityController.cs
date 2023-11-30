using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.Video;
using TMPro;

public class WebcamFeedReception : MonoBehaviour
{
    public RawImage rawImage;
    private SubscriberSocket subscriberSocket;
    private Canvas canvas;
        
    // Create a new GameObject for the RawImage
    private GameObject rawImageGO;
    // Add the RawImage component to the GameObject
            RawImage rawImageComponent;
    private Thread receiverThread;
    private Thread messageReceiverThread;//new
    private bool isReceivingFrames = false;
    private bool isReceivingMessages = false;

    void Start()
    {
        AsyncIO.ForceDotNet.Force();
        isReceivingFrames = true;
        isReceivingMessages=true;
        subscriberSocket = new SubscriberSocket();
        subscriberSocket.Connect("tcp://127.0.0.1:5555");
        subscriberSocket.Subscribe("ai_vr");
        canvas = GetComponent<Canvas>();
        rawImageGO = new GameObject("RawImageObject");
        
            // Add the RawImage component to the GameObject
            rawImageComponent = rawImageGO.AddComponent<RawImage>();

        receiverThread = new Thread(ReceiveFrames);
        messageReceiverThread=new Thread(ReceiveMessagesAsync);
        messageReceiverThread.Start();
        receiverThread.Start();
        Debug.Log("Receiver Initialised");
    }

    void OnDisable()
    {
        Debug.Log("On Disable Called");
        
        isReceivingFrames = false;
        isReceivingMessages=false;

        if (receiverThread != null && receiverThread.IsAlive)
        {
            receiverThread.Join();
            messageReceiverThread.Join();
        }

        subscriberSocket.Close();
        NetMQConfig.Cleanup();

    }

    

    
    async void ReceiveFrames()
    {
        using (var subscriber = new SubscriberSocket())
        {
            subscriber.Connect("tcp://127.0.0.1:5556");
            subscriber.Subscribe("");

            while (isReceivingFrames)
            {
                try
                {
                    byte[] frame = await Task.Run(() =>subscriber.ReceiveFrameBytes());

                    if (frame.SequenceEqual(new byte[] { (byte)'S', (byte)'T', (byte)'O', (byte)'P' }))
                    {
                         // Perform actions to stop the receiver
                        isReceivingFrames=false;
                        subscriber.Close();
                        Debug.Log("Connection Terminated from python");
                        
                    }

                    else if (frame != null && frame.Length > 0)
                    {
                        Debug.Log("At Thread Dispatcher");
                        UnityMainThreadDispatcher.Enqueue(() => {DisplayFrame(frame);});

                    }

                    else if (frame == null)
                    {
                        try
                        {
                            Debug.Log("Adding placeholder image");
                            Texture2D placeholder = Resources.Load<Texture2D>("placeholder");
                            rawImage.texture = placeholder;
                        }
                            catch (Exception ex)
                        {
                            Debug.LogError("Error displaying webcam frame: " + ex.Message);
                            }
                        
                    }

                    


                }
                catch (Exception ex)
                {
                    Debug.LogError("ERROR:receiving webcam frame: " + ex.Message);
                }
            }

            
        }
    }

    private async void ReceiveMessagesAsync()
    {
        
            
        while (isReceivingMessages)
        {
            try
            {
                // Receive a message as a string
                //string message = subscriberSocket.ReceiveFrameString();
                Debug.Log("Started");
                string message = await Task.Run(() => subscriberSocket.ReceiveFrameString());
                // Check if the received message starts with the subscribed topic
                Debug.Log("Message "+message);
                if (message.StartsWith("ai_vr "))
                {
                    // Extract the actual message content (remove the topic prefix)
                    string actualMessage = message.Substring("ai_vr ".Length);

                    // Use UnityMainThreadDispatcher to interact with Unity's main thread
                    HandleReceivedMessage(actualMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error receiving message: " + ex.Message);
            }

            //await Task.Yield();
        }
    
    }

    private void DisplayFrame(byte[] frameData)
    {
        Debug.Log("Frame Received");
        try
        {
            
            // Set the RawImage as a child of the Canvas
            rawImageGO.transform.SetParent(canvas.transform);


            
            // Set the RawImage properties (optional)
            rawImageComponent.color = Color.white; // Set color if needed


            RectTransform rectTransform = rawImageComponent.rectTransform;

            // Set the width and height
            rectTransform.sizeDelta = new Vector2(300f, 200f);
            
            rectTransform.anchoredPosition = new Vector2(350f,155f);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(frameData);

            rawImageComponent.texture = texture;
       
            
        }
        catch (Exception ex)
        {
            Debug.LogError("Error displaying webcam frame: " + ex.Message);
        }
    }

    private void HandleReceivedMessage(string message)
    {

        Debug.Log("Message Received: " + message);
        string[] messageParts = message.Split(' ');
        // Check if it's a valid message format
        foreach (string item in messageParts)
        {
            Debug.Log(item);
        }

        if (messageParts[0] == "launch_video")
            {
                string spawnPositionInput = messageParts[1];
                string scaleInput = messageParts[2];
                string file_path=messageParts[3];
                


                // Parse the spawn position input
                string[] spawnPositionValues = spawnPositionInput.Split(',');
                string[] scaleInputValues = scaleInput.Split(',');
                
                if (spawnPositionValues.Length == 3 && scaleInputValues.Length == 3)
                {
                    if (float.TryParse(spawnPositionValues[0], out float x) && float.TryParse(spawnPositionValues[1], out float y) &&
                    float.TryParse(spawnPositionValues[2], out float z) && float.TryParse(scaleInputValues[0], out float h)
                    && float.TryParse(scaleInputValues[1], out float w) && float.TryParse(scaleInputValues[2], out float l))
                    {

                        //LaunchVideo(x, y, z, h, w, l,file_path);
                        UnityMainThreadDispatcher.Enqueue(() => { LaunchVideo(x, y, z, h, w, l, file_path); });
                        return;
                    }
                }

            }

            else if (messageParts[0] == "create_cube")
            {
                string spawnPositionInput = messageParts[1];
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];


                // Parse the spawn position input
                
                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceCube(spawnPositionVector ,scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                    
                
            }

            else if (messageParts[0] == "create_plane")
            {
                string spawnPositionInput = messageParts[1];
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];


                // Parse the spawn position input
                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlacePlane(spawnPositionVector, scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                 
            }

            else if (messageParts[0] == "create_quad")
            {
                string spawnPositionInput = messageParts[1];
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];


                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceQuad(spawnPositionVector,scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                }
                   

            else if (messageParts[0] == "create_sphere")
            {
                string spawnPositionInput = messageParts[1];
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];
                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceSphere(spawnPositionVector,scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                    
            }

            else if (messageParts[0] == "create_capsule")
            {
                string spawnPositionInput = messageParts[1];
    
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];
                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceCapsule(spawnPositionVector,scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                    
            }
        else if (messageParts[0] == "create_cylinder")
            {
                string spawnPositionInput = messageParts[1];
    
                string scaleInput = messageParts[2];
                string objectName=messageParts[3];
                string objectColor=messageParts[4];
                string objectRotation=messageParts[5];
                Vector3 spawnPositionVector = vectorParserUtility(spawnPositionInput);
                Vector3 scaleVector = vectorParserUtility(scaleInput);
                string[] colorInputValues=objectColor.Split(',');
                Vector3 rotationVector = vectorParserUtility(objectRotation);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceCylinder(spawnPositionVector,scaleVector,rotationVector,objectName,colorInputValues); });
                return;
                  
            }



            else if (messageParts[0] == "text_addition")

        {   
            string textColor=messageParts[5];
            string[] colorInputValues=textColor.Split(',');
            if (float.TryParse(messageParts[1], out float x_coord) && float.TryParse(messageParts[2], out float y_coord) &&
                float.TryParse(messageParts[3], out float z_coord) && int.TryParse(messageParts[4], out int font_scale)
                )
            {
                string textToDisplay = string.Join(" ", messageParts, 6, messageParts.Length - 6);
                UnityMainThreadDispatcher.Enqueue(() => { PlaceText(x_coord, y_coord, z_coord, font_scale, textToDisplay,colorInputValues); });
                        
                
                return;
            }

        }

        else
        {
            Debug.LogError("Invalid message format.");
        }
        



    }

    private Vector3 vectorParserUtility(string valueTuple){

        string[] inputValues=valueTuple.Split(',');
        float.TryParse(inputValues[0], out float x);
        float.TryParse(inputValues[1], out float y);
        float.TryParse(inputValues[2], out float z);

        return new Vector3(x,y,z);


    }

    private void LaunchVideo(float x, float y, float z, float h, float w, float l, string videoFilePath)
    {

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "QuadWithVideo";
        quad.transform.position = new Vector3(x, y, z); // Set the position as needed
        quad.transform.localScale = new Vector3(h, w, l);
        VideoPlayer videoPlayer = quad.AddComponent<VideoPlayer>();

            // Set the video clip from file path
            if (!string.IsNullOrEmpty(videoFilePath) && File.Exists(videoFilePath))
            {
                videoPlayer.url = videoFilePath;
            }
            else
            {
                Debug.LogError("Invalid or empty video file path.");
                return;
            }

            // Set the video player to play on awake
            videoPlayer.playOnAwake = true;
            videoPlayer.isLooping = true;
            
            videoPlayer.Play();
        


    }


    private void PlaceSphere(Vector3 objPos, Vector3 objScale,Vector3 objRot, string object_name, string[] colorInputValues)
{
    Debug.Log("Received message 'PlaceSphere'. Creating a Sphere...");
    
    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    sphere.name = object_name;
    sphere.transform.position = objPos;

    Renderer sphereRenderer = sphere.GetComponent<Renderer>();
    if (colorInputValues.Length == 3)
    {
        if (float.TryParse(colorInputValues[0], out float r) &&
            float.TryParse(colorInputValues[1], out float g) &&
            float.TryParse(colorInputValues[2], out float b))
        {
            // Parsing was successful, and r, g, and b now contain the parsed values.
            // You can use these float variables in your code.
            Debug.Log($"R: {r}, G: {g}, B: {b}");
            Color newColor = new Color(r, g, b);
            sphereRenderer.material.color = newColor;
        }
        else
        {
            Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
            sphereRenderer.material.color = Color.red;
        }
    }
    else
    {
        Debug.LogError("The string array does not contain three elements, setting default color.");
        sphereRenderer.material.color = Color.red;
    }

    // Set the scale of the sphere based on the radius
    sphere.transform.rotation = Quaternion.Euler(objRot);
    sphere.transform.localScale = objScale;
}

private void PlaceCylinder(Vector3 objPos, Vector3 objScale,Vector3 objRot, string object_name, string[] colorInputValues)
{
    Debug.Log("Received message 'PlaceSphere'. Creating a Sphere...");
    
    GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    cylinder.name = object_name;
    cylinder.transform.position = spawnPosition;

    Renderer cylinderRenderer = cylinder.GetComponent<Renderer>();
    if (colorInputValues.Length == 3)
    {
        if (float.TryParse(colorInputValues[0], out float r) &&
            float.TryParse(colorInputValues[1], out float g) &&
            float.TryParse(colorInputValues[2], out float b))
        {
            // Parsing was successful, and r, g, and b now contain the parsed values.
            // You can use these float variables in your code.
            Debug.Log($"R: {r}, G: {g}, B: {b}");
            Color newColor = new Color(r, g, b);
            cylinderRenderer.material.color = newColor;
        }
        else
        {
            Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
            cylinderRenderer.material.color = Color.red;
        }
    }
    else
    {
        Debug.LogError("The string array does not contain three elements, setting default color.");
        cylinderRenderer.material.color = Color.red;
    }

    // Set the scale of the sphere based on the radius
    cylinder.transform.localScale = objScale;
    cylinder.transform.rotation = Quaternion.Euler(objRot);
}


 private void PlaceCapsule(Vector3 objPos, Vector3 objScale,Vector3 objRot, string object_name, string[] colorInputValues)
{
    Debug.Log("Received message 'PlaceCapsule'. Creating a Capsule...");
    
    GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
    capsule.name = object_name;
    capsule.transform.position = objPos;

    Renderer capsuleRenderer = capsule.GetComponent<Renderer>();
    if (colorInputValues.Length == 3)
    {
        if (float.TryParse(colorInputValues[0], out float r) &&
            float.TryParse(colorInputValues[1], out float g) &&
            float.TryParse(colorInputValues[2], out float b))
        {
            // Parsing was successful, and r, g, and b now contain the parsed values.
            // You can use these float variables in your code.
            Debug.Log($"R: {r}, G: {g}, B: {b}");
            Color newColor = new Color(r, g, b);
            capsuleRenderer.material.color = newColor;
        }
        else
        {
            Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
            capsuleRenderer.material.color = Color.red;
        }
    }
    else
    {
        Debug.LogError("The string array does not contain three elements, setting default color.");
        capsuleRenderer.material.color = Color.red;
    }

    // Set the scale of the sphere based on the radius
    capsule.transform.rotation = Quaternion.Euler(objRot);
    capsule.transform.localScale = objScale;
}

    private void PlacePlane(Vector3 objPos, Vector3 objScale,Vector3 objRot, string object_name, string[] colorInputValues)
    {

        Debug.Log("Received message 'PlacePlane'. Creating a Plane...");
        
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name=object_name;
        plane.transform.position = objPos;
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        if (colorInputValues.Length == 3)
        {
            if (float.TryParse(colorInputValues[0], out float r) &&
                float.TryParse(colorInputValues[1], out float g) &&
                float.TryParse(colorInputValues[2], out float b))
                
                {
                // Parsing was successful, and float1, float2, and float3 now contain the parsed values.
                // You can use these float variables in your code.
                Debug.Log($"R: {r}, G: {g}, B: {b}");
                Color newColor = new Color(r,g,b);
                planeRenderer.material.color =newColor;
                }
            else
                {
                Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
                planeRenderer.material.color = Color.red;
                }
        }
        else
            {
            Debug.LogError("The string array does not contain three elements, setting default color.");
            planeRenderer.material.color = Color.red;
            }
        


        plane.transform.localScale = objScale;
        plane.transform.rotation = Quaternion.Euler(objRot);
    }

    private void PlaceQuad(Vector3 objPos, Vector3 objScale,Vector3 objRot, string object_name, string[] colorInputValues)
    {

        Debug.Log("Received message 'PlaceQuad'. Creating a Quad...");
        Vector3 spawnPosition = new Vector3(x, y, z);
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name=object_name;
        quad.transform.position = spawnPosition;
        Renderer quadRenderer = quad.GetComponent<Renderer>();
        if (colorInputValues.Length == 3)
        {
            if (float.TryParse(colorInputValues[0], out float r) &&
                float.TryParse(colorInputValues[1], out float g) &&
                float.TryParse(colorInputValues[2], out float b))
                
                {
                // Parsing was successful, and float1, float2, and float3 now contain the parsed values.
                // You can use these float variables in your code.
                Debug.Log($"R: {r}, G: {g}, B: {b}");
                Color newColor = new Color(r,g,b);
                quadRenderer.material.color =newColor;
                }
            else
                {
                Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
                quadRenderer.material.color = Color.red;
                }
        }
        else
            {
            Debug.LogError("The string array does not contain three elements, setting default color.");
            quadRenderer.material.color = Color.red;
            }
        


        quad.transform.localScale = new Vector3(h, w, l);
        quad.transform.rotation = Quaternion.Euler(objRot);
    }

    private void PlaceCube(Vector3 objPos, Vector3 objScale, Vector3 objRot,string object_name, string[] colorInputValues)
    {

        Debug.Log("Received message 'PlaceCube'. Creating a Cube...");
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name=object_name;
        cube.transform.position = objPos;
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (colorInputValues.Length == 3)
        {
            if (float.TryParse(colorInputValues[0], out float r) &&
                float.TryParse(colorInputValues[1], out float g) &&
                float.TryParse(colorInputValues[2], out float b))
                
                {
                // Parsing was successful, and float1, float2, and float3 now contain the parsed values.
                // You can use these float variables in your code.
                Debug.Log($"R: {r}, G: {g}, B: {b}");
                Color newColor = new Color(r,g,b);
                cubeRenderer.material.color =newColor;
                }
            else
                {
                Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
                cubeRenderer.material.color = Color.red;
                }
        }
        else
            {
            Debug.LogError("The string array does not contain three elements, setting default color.");
            cubeRenderer.material.color = Color.red;
            }
        


        cube.transform.localScale = objScale;
        cube.transform.rotation = Quaternion.Euler(objRot);
    }

    private void PlaceText(float x_coord, float y_coord, float z_coord, int font_scale, string textToDisaply, string[] colorInputValues)
    {

        Debug.Log("Received message 'PlaceText'. Performing action 2...");
        GameObject newTextObject = new GameObject("NewTMPText");


        TextMeshPro newTextComponent = newTextObject.AddComponent<TextMeshPro>();


        newTextObject.transform.SetParent(transform);

        if (colorInputValues.Length == 3)
        {
            if (float.TryParse(colorInputValues[0], out float r) &&
                float.TryParse(colorInputValues[1], out float g) &&
                float.TryParse(colorInputValues[2], out float b))
                
                {
                // Parsing was successful, and float1, float2, and float3 now contain the parsed values.
                // You can use these float variables in your code.
                Debug.Log($"R: {r}, G: {g}, B: {b}");
                Color textColor = new Color(r,g,b);
                

                newTextComponent.color =textColor;

                }
            else
                {
                Debug.LogError("Parsing failed. One or more elements in the array are not valid float values, choosing default color.");
                newTextComponent.color = Color.red;
                }
        }
        else
            {
            Debug.LogError("The string array does not contain three elements, setting default color.");
            newTextComponent.color = Color.red;
            }
        


        newTextComponent.text = textToDisaply;
        newTextComponent.fontSize = font_scale;
        

        // Set the position of the TMP text
        //newTextObject.transform.position = new Vector3(x_coord, y_coord, z_coord);
        newTextComponent.transform.position = new Vector3(x_coord, y_coord, z_coord);
    }
}




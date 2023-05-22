/// <summary>
/// Author: AkilarLiao
/// Date: 2023/05/22
/// Desc:
/// </summary>
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections.LowLevel.Unsafe;

public class GPUInstanceTest : MonoBehaviour
{
    public void SwitchCameraTransform()
    {
        m_isSwitch = !m_isSwitch;
        if (m_isSwitch)
        {
            m_cameraTransform.position = new Vector3(0.0f, 2.0f, 0.0f);
            m_cameraTransform.eulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        }
        else
        {
            m_cameraTransform.position = m_originalCameraPosition;
            m_cameraTransform.eulerAngles = m_originalEulerAngles;
        }
    }

    private void OnEnable()
    {
        if (!m_prefab)
            return;
        m_cameraTransform = Camera.main.transform;
        m_originalCameraPosition = m_cameraTransform.position;
        m_originalEulerAngles = m_cameraTransform.eulerAngles;

        m_targetMaterial = m_prefab.GetComponent<MeshRenderer>().sharedMaterial;
        m_targetMesh = m_prefab.GetComponent<MeshFilter>().sharedMesh;

        if (m_mode == MODE.MANUAL_GUP_INSTANCE)
        {
            m_matrices = new Matrix4x4[m_rowCount * m_columnCount];
            m_targetMaterial.enableInstancing = true;
        }

        ModeAndDebugInfoSetting();        
        BuildObjects();
    }
    private void OnDisable()
    {
        var theURPPipelineAsset = (UniversalRenderPipelineAsset)
            GraphicsSettings.renderPipelineAsset;

        theURPPipelineAsset.supportsDynamicBatching = m_originalDynamicBathState;
        theURPPipelineAsset.useSRPBatcher = m_originalSRPBathState;
    }
    private void BuildObjects()
    {
        var rootTransform = transform;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        Matrix4x4 matrix = Matrix4x4.identity;
        for (int rowIndex = 0; rowIndex<m_rowCount; ++rowIndex)
        {
            position.z = rowIndex * m_stepSize;
            for (int columnIndex = 0; columnIndex<m_columnCount; ++columnIndex)
            {
                position.x = columnIndex * m_stepSize;

                if (m_mode == MODE.MANUAL_GUP_INSTANCE)
                {
                    matrix.SetColumn(3, new Vector4(position.x, position.y, position.z, 1.0f));
                    m_matrices[columnIndex + rowIndex * m_columnCount] = matrix;
                }
                else
                {
                    var go = Instantiate(m_prefab, rootTransform);
                    go.transform.position = position;
                    var meshRenderer = go. GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial.enableInstancing = m_mode == MODE.AUTO_GPU_INSTANCE;
                }
            }
        }
    }

    private void Update()
    {
        ProcesssDrawInstance();
    }

    private unsafe void ProcesssDrawInstance()
    {
        if (m_mode != MODE.MANUAL_GUP_INSTANCE)
            return;
        int availableCount = 0;
        for (int index = 0; index < m_matrices.Length; index += 1023)
        {
            availableCount = Mathf.Min(1023, m_matrices.Length - index);
            fixed (Matrix4x4* matrixArrayPointer = m_matrices)
            fixed (void* matrixArray1023Pointer = m_matrices1023)
            {
                UnsafeUtility.MemCpy(matrixArray1023Pointer,
                    matrixArrayPointer + index,
                    Mathf.Min(1023, availableCount) *
                    (long)UnsafeUtility.SizeOf<Matrix4x4>());
            }
            Graphics.DrawMeshInstanced(m_targetMesh, 0, m_targetMaterial,
                m_matrices1023, availableCount, null,
                ShadowCastingMode.Off, false, 0);
        }
    }


    private void OnGUI()
    {
        int width = Screen.width, height = Screen.height;
        var printRect = new Rect(0, 0,
            width, height * 2 / 100);
        m_style.fontSize = height * 5 / 100;

        m_tempText = string.Format("Mode:{0}", m_mode);
        GUI.Label(printRect, m_tempText, m_style);
    }

    private void ModeAndDebugInfoSetting()
    {
        //Disabling this lets you skip the GUI layout phase.
        //it' can avoid gc...
        useGUILayout = false;
        m_style = new GUIStyle();
        m_style.alignment = TextAnchor.UpperLeft;
        m_style.normal.textColor = m_textColor;

        var theURPPipelineAsset = (UniversalRenderPipelineAsset)
            GraphicsSettings.renderPipelineAsset;

        m_originalDynamicBathState = theURPPipelineAsset.supportsDynamicBatching;
        m_originalSRPBathState = theURPPipelineAsset.useSRPBatcher;

        switch (m_mode)
        {
            case MODE.DYNAMIC_BATCH:
                theURPPipelineAsset.supportsDynamicBatching = true;
                theURPPipelineAsset.useSRPBatcher = false;
                break;
            case MODE.SRP_BATCH:
                theURPPipelineAsset.supportsDynamicBatching = false;
                theURPPipelineAsset.useSRPBatcher = true;
                break;
            case MODE.AUTO_GPU_INSTANCE:
                theURPPipelineAsset.useSRPBatcher = false;
                break;
            case MODE.MANUAL_GUP_INSTANCE:
                break;
        }
    }

    public enum MODE
    {
        DYNAMIC_BATCH,
        SRP_BATCH,
        AUTO_GPU_INSTANCE,
        MANUAL_GUP_INSTANCE
    }
    [SerializeField]
    private uint m_rowCount = 100;
    [SerializeField]
    private uint m_columnCount = 100;
    [SerializeField]
    private float m_stepSize = 2.0f;

    [SerializeField]
    private MODE m_mode = MODE.DYNAMIC_BATCH;
    [SerializeField]
    private GameObject m_prefab = null;
    [SerializeField]
    private Color m_textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

    private bool m_originalDynamicBathState = false;
    private bool m_originalSRPBathState = false;
    private GUIStyle m_style = null;
    private string m_tempText;

    private Mesh m_targetMesh = null;
    private Material m_targetMaterial = null;
    private Matrix4x4[] m_matrices;
    private Matrix4x4[] m_matrices1023 = new Matrix4x4[1023];

    //private Matrix4x4 m_oritinalCameraMatrix = Matrix4x4.identity;
    private Vector3 m_originalCameraPosition = Vector3.zero;
    private Vector3 m_originalEulerAngles = Vector3.zero;
    private bool m_isSwitch = false;
    private Transform m_cameraTransform = null;
}

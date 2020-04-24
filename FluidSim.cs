using UnityEngine;
using System.Collections;
//using UnityEngine.UI;


namespace FluidSim2DProject
{

    public class FluidSim : MonoBehaviour
    {

        public Color m_fluidColor = Color.red;  //流体颜色

        public Color m_obstacleColor = Color.white; //障碍物颜色
      
        public Material m_guiMat, m_advectMat, m_buoyancyMat, m_divergenceMat, m_jacobiMat, m_impluseMat, m_gradientMat, m_obstaclesMat;
        //                gui材质    平流材质        浮力           散度              雅各比         冲力         梯度            障碍物


        RenderTexture m_guiTex, m_divergenceTex, m_obstaclesTex;
        //              gui         散度               障碍物
        RenderTexture[] m_velocityTex, m_densityTex, m_pressureTex, m_temperatureTex;
        //                速度            密度           压力            温度


        //=============================================================================
        public Material m_transformMat;
        //public Material m_noiseMat;
        //RenderTexture[] m_noiseTex;

        //=============================================================================

        float m_timeStep = 0.125f;  //时间步
        float m_impulseTemperature = 10.0f;  //脉冲温度
        float m_impulseDensity = 1.0f;  //脉冲密度

        //float m_temperatureDissipation = 0.99f;  //温度耗散
        //float m_velocityDissipation = 0.99f;    //速度耗散
        //float m_densityDissipation = 0.9999f; //密度耗散
        float m_temperatureDissipation = 1.0f;  //温度耗散
        float m_velocityDissipation = 1.0f;    //速度耗散
        float m_densityDissipation = 1.0f; //密度耗散

        float m_ambientTemperature = 0.0f;  //环境温度
        public float m_smokeBuoyancy = 1.0f;   //烟雾浮力
        float m_smokeWeight = 0.05f;    //烟重

        float m_cellSize = 1.0f;    //单元格大小
        float m_gradientScale = 1.0f;   //梯度比例

        Vector2 m_inverseSize;  //逆尺寸
        int m_numJacobiIterations = 50; //雅各比迭代数量

        Vector2 m_implusePos = new Vector2(0.5f, 0.0f); //脉冲坐标
        float m_impluseRadius = 0.1f;  //脉冲半径
        float m_mouseImpluseRadius = 0.05f; //鼠标脉冲半径

        //Vector2 m_obstaclePos = new Vector2(0.5f, 0.5f);    //障碍物坐标
        public Vector2 m_obstaclePos = new Vector2(1.0f, 1.0f);    //障碍物坐标
        float m_obstacleRadius = 0.1f;  //障碍物半径

        GUITexture m_gui;

        int m_width, m_height;
        Vector2 m_offset;

        void Start()
        {

            m_gui = GetComponent<GUITexture>();

            m_width = (int)m_gui.pixelInset.width;
            m_height = (int)m_gui.pixelInset.height;
            m_offset = new Vector2(m_gui.pixelInset.x, m_gui.pixelInset.y);

            m_inverseSize = new Vector2(1.0f / m_width, 1.0f / m_height);//逆尺寸

            m_velocityTex = new RenderTexture[2];//速度
            m_densityTex = new RenderTexture[2];//密度
            m_temperatureTex = new RenderTexture[2];//温度
            m_pressureTex = new RenderTexture[2];//压力

            //创建帧缓冲（2张纹理附件）
            //CreateSurface(m_velocityTex, RenderTextureFormat.RGFloat, FilterMode.Point);
            CreateSurface(m_velocityTex, RenderTextureFormat.RGFloat, FilterMode.Bilinear);//双线性插值
            CreateSurface(m_densityTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_temperatureTex, RenderTextureFormat.RFloat, FilterMode.Bilinear);
            CreateSurface(m_pressureTex, RenderTextureFormat.RFloat, FilterMode.Point);

            //=====================================================================================
            //m_noiseTex = new RenderTexture[2];//速度
            //CreateSurface(m_noiseTex, RenderTextureFormat.RGB111110Float, FilterMode.Point);


            //Graphics.Blit(null, m_noiseTex[0], m_noiseMat);
            //=====================================================================================

            //gui
            m_guiTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.ARGB32);
            m_guiTex.filterMode = FilterMode.Bilinear;
            m_guiTex.wrapMode = TextureWrapMode.Clamp;
            m_guiTex.Create();

            //散度
            m_divergenceTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_divergenceTex.filterMode = FilterMode.Point;
            m_divergenceTex.wrapMode = TextureWrapMode.Clamp;
            m_divergenceTex.Create();

            //障碍物
            m_obstaclesTex = new RenderTexture(m_width, m_height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            m_obstaclesTex.filterMode = FilterMode.Point;
            m_obstaclesTex.wrapMode = TextureWrapMode.Clamp;
            m_obstaclesTex.Create();

            GetComponent<GUITexture>().texture = m_guiTex;

            m_guiMat.SetTexture("_Obstacles", m_obstaclesTex);

        }

        //创建帧缓冲（2张纹理附件）
        void CreateSurface(RenderTexture[] surface, RenderTextureFormat format, FilterMode filter)
        {
            surface[0] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[0].filterMode = filter;
            surface[0].wrapMode = TextureWrapMode.Clamp;
            surface[0].Create();

            surface[1] = new RenderTexture(m_width, m_height, 0, format, RenderTextureReadWrite.Linear);
            surface[1].filterMode = filter;
            surface[1].wrapMode = TextureWrapMode.Clamp;
            surface[1].Create();
        }

        //平流
        void Advect(RenderTexture velocity, RenderTexture source, RenderTexture dest, float dissipation)
        {
            m_advectMat.SetVector("_InverseSize", m_inverseSize);
            m_advectMat.SetFloat("_TimeStep", m_timeStep);
            m_advectMat.SetFloat("_Dissipation", dissipation);
            m_advectMat.SetTexture("_Velocity", velocity);
            m_advectMat.SetTexture("_Source", source);
            m_advectMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_advectMat); //绘制到dest
        }

        //应用浮力
        void ApplyBuoyancy(RenderTexture velocity, RenderTexture temperature, RenderTexture density, RenderTexture dest)
        {

            m_buoyancyMat.SetTexture("_Velocity", velocity);
            m_buoyancyMat.SetTexture("_Temperature", temperature);
            m_buoyancyMat.SetTexture("_Density", density);
            m_buoyancyMat.SetFloat("_AmbientTemperature", m_ambientTemperature);
            m_buoyancyMat.SetFloat("_TimeStep", m_timeStep);
            m_buoyancyMat.SetFloat("_Sigma", m_smokeBuoyancy);
            m_buoyancyMat.SetFloat("_Kappa", m_smokeWeight);

            Graphics.Blit(null, dest, m_buoyancyMat);
        }

        //施加脉冲
        void ApplyImpulse(RenderTexture source, RenderTexture dest, Vector2 pos, float radius, float val)
        {

            m_impluseMat.SetVector("_Point", pos);
            m_impluseMat.SetFloat("_Radius", radius);
            m_impluseMat.SetFloat("_Fill", val);
            m_impluseMat.SetTexture("_Source", source);

            Graphics.Blit(null, dest, m_impluseMat);

        }

        //计算散度
        void ComputeDivergence(RenderTexture velocity, RenderTexture dest)
        {

            m_divergenceMat.SetFloat("_HalfInverseCellSize", 0.5f / m_cellSize);
            m_divergenceMat.SetTexture("_Velocity", velocity);
            m_divergenceMat.SetVector("_InverseSize", m_inverseSize);
            m_divergenceMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_divergenceMat);
        }

        //雅各比
        void Jacobi(RenderTexture pressure, RenderTexture divergence, RenderTexture dest)
        {

            m_jacobiMat.SetTexture("_Pressure", pressure);
            m_jacobiMat.SetTexture("_Divergence", divergence);
            m_jacobiMat.SetVector("_InverseSize", m_inverseSize);
            m_jacobiMat.SetFloat("_Alpha", -m_cellSize * m_cellSize);
            m_jacobiMat.SetFloat("_InverseBeta", 0.25f);
            m_jacobiMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_jacobiMat);
        }

        //减梯度
        void SubtractGradient(RenderTexture velocity, RenderTexture pressure, RenderTexture dest)
        {
            m_gradientMat.SetTexture("_Velocity", velocity);
            m_gradientMat.SetTexture("_Pressure", pressure);
            m_gradientMat.SetFloat("_GradientScale", m_gradientScale);
            m_gradientMat.SetVector("_InverseSize", m_inverseSize);
            m_gradientMat.SetTexture("_Obstacles", m_obstaclesTex);

            Graphics.Blit(null, dest, m_gradientMat);
        }

        //加障碍物
        void AddObstacles()
        {
            m_obstaclesMat.SetVector("_InverseSize", m_inverseSize);
            m_obstaclesMat.SetVector("_Point", m_obstaclePos);
            m_obstaclesMat.SetFloat("_Radius", m_obstacleRadius);

            Graphics.Blit(null, m_obstaclesTex, m_obstaclesMat);
        }

        //清理
        void ClearSurface(RenderTexture surface)
        {
            Graphics.SetRenderTarget(surface);
            GL.Clear(false, true, new Color(0, 0, 0, 0));
            Graphics.SetRenderTarget(null);
        }

        //交换
        void Swap(RenderTexture[] texs)
        {
            RenderTexture temp = texs[0];
            texs[0] = texs[1];
            texs[1] = temp;
        }



        //转换
        //===============================================
        //public Material m_transformMat;

        void RealTransform(RenderTexture velocity, RenderTexture dest)
        {
            m_transformMat.SetTexture("_Velocity", velocity);

            Graphics.Blit(null, dest, m_transformMat);
        }
        //===============================================


        void Update()
        {
            //除非更改，否则障碍仅需添加一次。Obstacles only need to be added once unless changed.
            AddObstacles();//添加障碍物

            //设置浓度场和障碍物颜色。Set the density field and obstacle color.
            m_guiMat.SetColor("_FluidColor", m_fluidColor);
            m_guiMat.SetColor("_ObstacleColor", m_obstacleColor);

            int READ = 0;
            int WRITE = 1;

            //更新速度 Advect velocity against its self
            Advect(m_velocityTex[READ], m_velocityTex[READ], m_velocityTex[WRITE], m_velocityDissipation);
            //更新温度 Advect temperature against velocity
            Advect(m_velocityTex[READ], m_temperatureTex[READ], m_temperatureTex[WRITE], m_temperatureDissipation);
            //更新密度 Advect density against velocity
            Advect(m_velocityTex[READ], m_densityTex[READ], m_densityTex[WRITE], m_densityDissipation);


            //================================
            ////更新噪声
            //Advect(m_velocityTex[READ], m_noiseTex[READ], m_noiseTex[WRITE], m_densityDissipation);

            //Swap(m_noiseTex);
            //================================



            Swap(m_velocityTex);
            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //确定流体的流动如何改变速度 Determine how the flow of the fluid changes the velocity
            ApplyBuoyancy(m_velocityTex[READ], m_temperatureTex[READ], m_densityTex[READ], m_velocityTex[WRITE]);

            Swap(m_velocityTex);

            //刷新密度和温度的脉冲 Refresh the impluse of density and temperature
            ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], m_implusePos, m_impluseRadius, m_impulseTemperature);
            ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], m_implusePos, m_impluseRadius, m_impulseDensity);

            Swap(m_temperatureTex);
            Swap(m_densityTex);

            //如果左键单击添加脉冲，如果右键单击从鼠标位置删除脉冲。If left click down add impluse, if right click down remove impulse from mouse pos.
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Vector2 pos = Input.mousePosition;

                pos.x -= Screen.width * 0.5f;
                pos.y -= Screen.height * 0.5f;

                pos -= m_offset;

                pos.x /= m_width - 1.0f;
                pos.y /= m_height - 1.0f;

                float sign = (Input.GetMouseButton(0)) ? 1.0f : -1.0f;

                ApplyImpulse(m_temperatureTex[READ], m_temperatureTex[WRITE], pos, m_mouseImpluseRadius, m_impulseTemperature);
                ApplyImpulse(m_densityTex[READ], m_densityTex[WRITE], pos, m_mouseImpluseRadius, m_impulseDensity * sign);

                Swap(m_temperatureTex);
                Swap(m_densityTex);
            }

            //计算速度的散度 Calculates how divergent the velocity is
            ComputeDivergence(m_velocityTex[READ], m_divergenceTex);

            ClearSurface(m_pressureTex[READ]);

            int i = 0;
            for (i = 0; i < m_numJacobiIterations; ++i)
            {
                Jacobi(m_pressureTex[READ], m_divergenceTex, m_pressureTex[WRITE]);
                Swap(m_pressureTex);
            }


            


            //使用最后渲染到的压力tex 计算出无散速度 Use the pressure tex that was last rendered into. This computes divergence free velocity
            SubtractGradient(m_velocityTex[READ], m_pressureTex[READ], m_velocityTex[WRITE]);

            Swap(m_velocityTex);


            //粗粒转换
            //================================
            //RealTransform(m_velocityTex[READ], m_velocityTex[WRITE]);


            ////
            ////Graphics.Blit(null, m_guiTex, m_transform);//渲染到m_guiTex
            ////

            //Swap(m_velocityTex);
            //================================



            //渲染到guiTex
            Graphics.Blit(m_densityTex[READ], m_guiTex, m_guiMat);
            //Graphics.Blit(m_pressureTex[READ], m_guiTex, m_guiMat);
            //Graphics.Blit(m_velocityTex[READ], m_guiTex, m_guiMat);

        }
    }

}

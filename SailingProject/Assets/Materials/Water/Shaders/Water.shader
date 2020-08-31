Shader "Custom/Water"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend One OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Greater
                Pass IncrSat
            }
        }
    }

    FallBack "Water/Water"
}
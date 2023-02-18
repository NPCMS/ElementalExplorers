struct IndirectShaderData
{
    float4x4 PositionMatrix;
    float4x4 InversePositionMatrix;
    //float4 ControlData;
};

StructuredBuffer<IndirectShaderData> VisibleShaderDataBuffer;

void GetData_float(float InstanceID, out float4x4 PositionMatrix)
{
    PositionMatrix = VisibleShaderDataBuffer[InstanceID].PositionMatrix;
}
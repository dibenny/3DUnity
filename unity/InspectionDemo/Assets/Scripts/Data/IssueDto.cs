using System;
using UnityEngine;

namespace InspectionDemo
{
    /// <summary>问题在场景中的世界坐标，字段名与 Go 的 model.Position 一致。</summary>
    [Serializable]
    public class PositionDto
    {
        public float x;
        public float y;
        public float z;

        public PositionDto() { }

        public PositionDto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public PositionDto(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>后端返回的完整问题对象，字段名与 Go 的 model.Issue 一致。</summary>
    [Serializable]
    public class IssueDto
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public PositionDto position;
        public string createdAt;
        public string updatedAt;
    }

    /// <summary>POST /api/issues 的请求体。</summary>
    [Serializable]
    public class CreateIssueDto
    {
        public string title;
        public string description;
        public string priority;
        public PositionDto position;
    }

    /// <summary>PATCH /api/issues/{id}/status 的请求体。</summary>
    [Serializable]
    public class UpdateStatusDto
    {
        public string status;

        public UpdateStatusDto() { }

        public UpdateStatusDto(string status)
        {
            this.status = status;
        }
    }

    /// <summary>后端统一错误体：{"error":{"code":"...","message":"..."}}。</summary>
    [Serializable]
    public class ErrorBodyDto
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class ErrorResponseDto
    {
        public ErrorBodyDto error;
    }
}

namespace InspectionDemo
{
    /// <summary>Wrapper used to parse a top-level JSON array with JsonUtility.</summary>
    [System.Serializable]
    public class IssueListResponse
    {
        public IssueDto[] items;
    }
}

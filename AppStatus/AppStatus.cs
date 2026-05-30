namespace LaboratoryTestRequestManagementSystem.AppStatus
{
    public enum UserRole
    {
        LaboratoryManager,
        LabTechnician,
        Doctor,
        Patient,
        Admin
    }


  


    public enum Status
    {
        Active,
        Inactive
    }


    public enum OrderStatus
    {
        Ordered,
        PartiallyComplete,
        Complete,
        Cancelled
    }

    public enum OrderItemStatus
    {
        Ordered,
        Received,
        Cancelled
    }

    public enum Urgency
    {
        Routine,
        Urgent,
        Stat
    }

    public enum RequestStatus
    {
        Submitted,
        SamplesReceived,
        InProgress,
        Completed,
        Verified,
        ToBeReviewed,
        ReleasedByDoctor,
        Cancelled
    }


    public enum AccessRequestStatus
    {
        Pending,
        Granted,
        Denied
    }



}

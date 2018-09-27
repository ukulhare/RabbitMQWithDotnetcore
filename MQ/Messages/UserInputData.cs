﻿namespace MQ.Messages
{
    public abstract class UserInputData
    {
        public string Login { get; set; }

        public string Password { get; set; }
    }

    public class DocmentTwoPublishUserInputData : UserInputData
    {
        public string RegistryNumber { get; set; }
    }
}
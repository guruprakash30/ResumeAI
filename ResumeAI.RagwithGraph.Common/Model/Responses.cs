using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeAI.RagwithGraph.Common.Model
{
        public enum ExecutionState
        {
            Pending = 1,
            Success = 2,
            Failure = 3
        }
        public class OperationResult<T>
        {
            public ExecutionState ExecutionState { get; private set; } = ExecutionState.Pending;
            public T? Data { get; private set; }
            public OperationResult<T> Success(T data)
            {
                ExecutionState = ExecutionState.Success;
                Data = data;
                return this;
            }
            public OperationResult<T> Success()
            {
                ExecutionState = ExecutionState.Success;
                return this;
            }
            public OperationResult<T> Failure()
            {
                ExecutionState = ExecutionState.Failure;
                return this;
            }
            public OperationResult<T> Failure(T data)
            {
                ExecutionState = ExecutionState.Failure;
                Data = data;
                return this;
            }

        }


        public class OperationResult
        {
            public ExecutionState ExecutionState { get; private set; } = ExecutionState.Pending;

            public OperationResult Success()
            {
                ExecutionState = ExecutionState.Success;
                return this;
            }

            public OperationResult Failure()
            {
                ExecutionState = ExecutionState.Failure;
                return this;
            }
        }
}

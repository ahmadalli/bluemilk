﻿using System.Collections.Generic;
using BlueMilk.Codegen;
using BlueMilk.Codegen.Frames;
using BlueMilk.Codegen.Variables;
using BlueMilk.Compilation;
using BlueMilk.IoC.Instances;

namespace BlueMilk.IoC.Frames
{
    
    
    public class GetInstanceFrame : SyncFrame
    {
        private Variable _scope;

        public GetInstanceFrame(Instance instance)
        {
            Variable = new ServiceVariable(instance, this);
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.Write($"var {Variable.Usage} = {_scope}.{nameof(Scope.GetInstance)}({Variable.VariableType.FullNameInCode()});");
        }

        public override IEnumerable<Variable> FindVariables(IMethodVariables chain)
        {
            _scope = chain.FindVariable(typeof(Scope));
            yield return _scope;
        }
        
        public ServiceVariable Variable { get; }
    }
}
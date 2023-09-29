﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameEngine.Component
{
    public class GameObject
    {

        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public string ModelName { get; set; }

        public string Content { get; set; }

        public string Script { get; set; }

        public readonly ObservableCollection<Component> Components = new ObservableCollection<Component>();

        //public ReadOnlyObservableCollection<Component> Components { get; }

        public GameObject(string content)
        {
            Content = content;
        }

        public override string ToString()
        {
            return Content.ToString();
        }

        public void AddModel(string modelName)
        {
            Model model = new Model(this);
            model.ModelName = modelName;
            Components.Add(model);
        }
    }
}

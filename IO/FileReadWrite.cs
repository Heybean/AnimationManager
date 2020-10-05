﻿using AnimationManager.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AnimationManager.IO
{
    public class FileReadWrite
    {
        public static TextureAtlasViewModel Read(string filename)
        {
            var viewModel = new TextureAtlasViewModel();

            return viewModel;
        }

        public static void Write(string filename, TextureAtlasViewModel viewModel)
        {
            var directory = Path.GetDirectoryName(filename);

            // Find the relative paths for each texture atlas
            foreach(var atlas in viewModel.TextureAtlases)
            {
                atlas.RelativePath = Path.GetRelativePath(directory, atlas.Filename);
            }

            using (var writer = new XmlTextWriter(filename, null))
            {
                writer.WriteStartElement("Animations");
                foreach(var atlas in viewModel.TextureAtlases)
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartElement("Atlas");
                    writer.WriteAttributeString("file", atlas.RelativePath);
                    WriteChildren(writer, atlas.Children);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private static void WriteChildren(XmlTextWriter writer, IList<object> list)
        {
            foreach (var child in list)
            {
                if (child is TextureAtlasItem folder)
                {
                    writer.WriteStartElement("Folder");
                    writer.WriteAttributeString("name", folder.Name);
                    WriteChildren(writer, folder.Children);
                    writer.WriteEndElement();
                }
                else if (child is WpfSprite sprite)
                {
                    writer.WriteStartElement("Sprite");
                    writer.WriteAttributeString("name", sprite.Name);
                    writer.WriteAttributeString("fps", sprite.FPS.ToString());
                    // Origin X
                    if (sprite.HorizontalAlignment == SpriteHorizontalAlignment.Custom)
                    {
                        writer.WriteAttributeString("originx", sprite.OriginX.ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("originx", sprite.HorizontalAlignment.ToString());
                    }

                    // Origin Y
                    if (sprite.VerticalAlignment == SpriteVerticalAlignment.Custom)
                    {
                        writer.WriteAttributeString("originy", sprite.OriginY.ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("originy", sprite.VerticalAlignment.ToString());
                    }
                    writer.WriteEndElement();
                }
            }
        }
    }
}

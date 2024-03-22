import json
import re
# JSON文件路径
json_file_path = 'content.json'

# 输出文本文件路径
output_file_path = 'i18n/zh.json'
section = []
output_dict = {}



# 打开JSON文件并读取数据
with open(json_file_path, 'r',encoding='utf-8') as json_file:
    config = json.load(json_file)
    
def print_dict(name,obj):
    for key, value in obj.items():
        text = ""
        if isinstance(value, dict):
          output_dict["config."+key+".name"] = key
          print_dict(key,value)
        else:
            if key == "Section":
                if  not value in section:
                    section.append(value)
                    text = "config.section."+value+".name"
            elif key == "Description":
                text = "config."+name+".description"
            elif key == "AllowValues":
                items = value.split(',')
                for item in items:
                    
                    text_value = "config."+name+".values."+item.strip()
                    output_dict[text_value] = item.strip()                   
            if text:
               output_dict[text] = value
        
    
    
with open(output_file_path, 'w') as output_file1:
    print_dict("config",config)
    json.dump(output_dict,output_file1,indent=4,ensure_ascii=False)

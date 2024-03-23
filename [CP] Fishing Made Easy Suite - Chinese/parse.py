import json
import re
# JSON文件路径
content_file_path = 'content.json'
# 输出翻译文件路径
output_file_path = 'i18n/zh.json'

section = []
output_dict = {}


# 打开JSON文件并读取数据


def open_file(file_path):
    items = {}
    try:
        with open(file_path,'r',encoding='utf-8') as f:
            items = json.load(f)
    except json.JSONDecodeError:
        print("JSON解码错误，文件内容可能不是有效的JSON格式。")
    except FileNotFoundError:
        print("文件未找到，请检查文件路径是否正确。")
    except Exception as e:
    # 捕获其他所有类型的异常
        print(f"发生了未知异常: {e}")
        exit()
    finally:
        return items



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


config = open_file(content_file_path)
old = open_file(output_file_path)

print_dict("config",config)

for key , value in old.items():
    if output_dict[key]:
        output_dict[key] = value

    
    
    
with open(output_file_path, 'w',encoding='utf-8') as output_file1:
    json.dump(output_dict,output_file1,indent=4,ensure_ascii=False)

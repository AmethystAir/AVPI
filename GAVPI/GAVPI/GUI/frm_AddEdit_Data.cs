﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GAVPI
{
    public partial class frm_AddEdit_Data : Form
    {
        Data data_to_edit;

        public frm_AddEdit_Data()
        {
            InitializeComponent();
            populate_fields();
        }
        public frm_AddEdit_Data(Data data_item)
        {
            InitializeComponent();
            this.data_to_edit = data_item;
            populate_fields();
        }
        private void populate_fields()
        {
            cbDataType.DataSource = Data.Data_Types;
            if (data_to_edit != null)
            {
                txtDataName.Text = data_to_edit.name;
                
                txtDataValue.Text = data_to_edit.value.ToString();
                
                txtDataComment.Text = data_to_edit.comment;

                cbDataType.SelectedItem = data_to_edit.type.ToString();
            }
        }


        private void btnTriggerOk_Click(object sender, EventArgs e)
        {
            string data_name = txtDataName.Text.Trim();
            string data_type = "GAVPI." + cbDataType.SelectedItem.ToString();
            string data_value = txtDataValue.Text.Trim();
            string data_comment = txtDataComment.Text.Trim();

            // Basic Validation
            if ( (data_name.Length  == 0) || 
                 (data_value.Length == 0) ||
                 (data_type.Length  == 0))
            {
                MessageBox.Show("Data name, type or value cannot be blank.");				
                return;
            }


            // Case 1: New Data Item
            if (data_to_edit == null)
            {
                // Check if data with this name already exists
                if (GAVPI.Profile.ProfileDB.isDataNameTaken(data_name))
                {
                    MessageBox.Show("A data item with this name already exisits.");
                    return;
                }
                else
                {
                    /*
                     * Data.validate returns true if it is possible to call ?.Parse on the data value
                     * We then use reflection to dynamically call the appropriate cast method of Data
                     * and return the proper object type.
                     * 
                     */
                    
                    /*
                     *  I leave the try catch redudantly, I think I have all execution branches 
                     *  covered so that it is not needed, however it is probably good practice
                     *  to sandwhich all calls to Data initialization in try catch
                     *  this is because ToObject will call (?).Parse
                     */
                    try
                    {
                        if (Data.validate(data_type, data_value))
                        {
                            // ex: GAVPI.INT 
                            Type new_data_type = Type.GetType(data_type);

                            // (ToObject) is static method which will cast a string value to 
                            // the appropriate value type.
                            MethodInfo method = new_data_type.GetMethod("ToObject");

                            // invokes the static method, cast_data_value will match value of Data, ex: INT.value is an int
                            object cast_data_value = method.Invoke(null, new string[] { data_value }); 

                            object data_instance = Activator.CreateInstance(new_data_type, data_name, cast_data_value, data_comment);

                            GAVPI.Profile.ProfileDB.Insert((Data)data_instance);
                        }
                        else
                        {
                            invalid_data_value_msg(data_value);
                            return;
                        }
                    }
                    catch
                    {
                        invalid_data_value_msg(data_value);
                        return;
                    }
                }
                
            }
            // Case 2: Existing item (edit mode)
            else
            {
                data_to_edit.type = cbDataType.SelectedItem.ToString();
                data_to_edit.comment = data_comment;

                // Now cast, this is necessary if the type has changed.
                try
                {
                    Type new_data_type = Type.GetType(data_type);

                    if (Data.validate(data_type, data_value))
                    {
                        // ex: GAVPI.INT 
                        Type thisType = Type.GetType(data_type);

                        // (ToObject) is static method which will cast a string value to 
                        // the appropriate value type.
                        MethodInfo method = thisType.GetMethod("ToObject");

                        // invokes the static method, cast_data_value will match value of Data, ex: INT.value is an int
                        object cast_data_value = method.Invoke(null, new string[] { data_value });

                        data_to_edit.value = cast_data_value;

                        //object data_instance = Activator.CreateInstance(new_data_type, data_name, cast_data_value, data_comment);
                        //GAVPI.Profile.ProfileDB.Insert((Data)data_instance);
                    }
                    else
                    {
                        invalid_data_value_msg(data_value);
                        return;
                    }
                }
                catch
                {
                    invalid_data_value_msg(data_value);
                    return;
                }
                // end-cast


                if (data_to_edit.name == data_name)
                {
                    // The current data element name is unchanged.
                    GAVPI.Profile.ProfileDB.DB[data_name] = data_to_edit;
                }
                else
                {
                    // The name has been changed, check if the name is taken.
                    if (GAVPI.Profile.ProfileDB.isDataNameTaken(data_name))
                    {
                        // Name is taken.
                        MessageBox.Show("A data element with this name already exists.");
                        return;
                    }
                    else
                    {
                        // There is no way to edit the key, remove the old entry and make a new one.

                        // BUG : Action.value will not be updated automatically here
                        GAVPI.Profile.ProfileDB.DB.Remove(data_name);
                        data_to_edit.name = data_name;
                        GAVPI.Profile.ProfileDB.Insert(data_to_edit);
                    }
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnTriggerCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        private void invalid_data_value_msg(string data_value)
        {
            MessageBox.Show(data_value + " is not a valid value for the selected data type.");
        }
        //private 
    }
}

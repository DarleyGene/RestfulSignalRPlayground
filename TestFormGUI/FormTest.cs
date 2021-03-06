﻿using Microsoft.AspNet.SignalR.Client;
using Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestBusinessLogic.APIConsumption.TestServiceREST;
using TestBusinessLogic.Models;
using TestFormGUI;

namespace TestFormGUI
{
    public partial class FormTest : Form
    {
        #region Properties and Fields
        // Set the hub values before continuing.
        // The hub could potentially be injected, but this is a small application, so I don't feel it necessary.
        private const string _hubURI = "http://localhost:2681/";
        private const string _hubName = "Test";
        private static readonly HubConnection _connection = new HubConnection(_hubURI);
        private static readonly IHubProxy _hub = _connection.CreateHubProxy(_hubName);

        private BindingList<TestModel> data = new BindingList<TestModel>();
        #endregion

        #region Construction and Initialization
        public FormTest()
        {
            InitializeComponent();

            // Calling asynchronously as the hub in this application is only used for broadcasts between users
            // I'm typically going to be the only user. 
            InitializeHub();
            InitializeData();
            bindingTestData.DataSource = data;
        }

        private async void InitializeHub()
        {
            // Subscribe to hub events
            _hub.On<byte[]>("AddTestModel", model => this.Invoke(new MethodInvoker(() => { addTestModel(model); })));
            _hub.On<int>("RemoveTestModel", modelID => this.Invoke(new MethodInvoker(() => { removeTestModel(modelID); })));
            // Start the Hub. Using a task to synchronously 
            await _connection.Start();
        }

        private async Task InitializeData() => await getAllData();
        #endregion

        #region Form Events
        private async void btnGetAll_Click(object sender, EventArgs e) =>  await getAllData();

        private void btnGetID_Click(object sender, EventArgs e)
        {
            bindingTestData.SuspendBinding();

            foreach(DataGridViewRow row in gridModels.Rows)
                row.Visible = ((TestModel)row.DataBoundItem).ID == (int)num.Value;

            bindingTestData.ResumeBinding();
        }

        // TODO: Input Validation.... Not that it's important in this app.
        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            TestModel model = getFormTestModel();
            await RESTClient.Instance.InsertModelAsync(model);
            clearInput();
        }
        #endregion

        #region Private Helper Methods
        private void clearInput()
        {
            txtFirstName.Text =
                txtLastName.Text =
                txtPhone.Text = string.Empty;
        }

        private async Task getAllData()
        {
            lblRetreiving.Visible = true;

            //Since the binding list only manipulates one item at a time...
            // Reset the list by removing everything.
            while (data.Count > 0)
                data.Remove(data[0]);
            // Add all the current items from 
            foreach (TestModel model in await RESTClient.Instance.GetTestModelsAsync())
                data.Add(model);

            lblRetreiving.Visible = false;
        }

        private TestModel getFormTestModel() => new TestModel()
        {
            ID = data.Max(m => m.ID) + 1,
            FirstName = txtFirstName.Text,
            LastName = txtLastName.Text,
            PhoneNumber = txtPhone.Text
        };

        private void addTestModel(byte[] model) =>
            data.Add(ProtoSerializer.Deserialize<TestModel>(model));

        private void removeTestModel(int id) =>
            data.Remove(data.Where(d => d.ID == id).FirstOrDefault());
        #endregion
    }
}

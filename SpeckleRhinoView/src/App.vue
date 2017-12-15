<template>
  <div id="app">
    <v-app >
      <v-tabs grow v-model='active'>
        <v-tabs-bar class='transparent' dark>
          <v-tabs-item key='clients' href='clients'>
            Clients
          </v-tabs-item>
          <v-tabs-item key='accounts' href='accounts'>
            Accounts
          </v-tabs-item>
          <v-menu open-on-hover transition="slide-x-transition">
            <v-tabs-item slot='activator' @click.native='showDev'>
              <v-icon style='font-size: 14px;'>code</v-icon>
            </v-tabs-item>
            <v-tabs-item slot='activator' @click.native='purgeClients'>
              <v-icon style='font-size: 14px;'>refresh</v-icon>
            </v-tabs-item>
          </v-menu>
          <v-tabs-slider color='grey'></v-tabs-slider>
        </v-tabs-bar>
        <v-tabs-items>
          <v-tabs-content id='clients' key='clients'>
            <v-card flat class='transparent'>
              <client-manager></client-manager>
            </v-card>
          </v-tabs-content>
          <v-tabs-content id='accounts' key='accounts'>
            <v-card flat>
              <accounts-manager></accounts-manager>
            </v-card>
          </v-tabs-content>
        </v-tabs-items>
      </v-tabs>
      <v-fab-transition>
        <v-speed-dial v-model='fab' hover fixed bottom right direction='top' v-show='active=="clients"'>
          <v-btn slot='activator' fab v-model='fab' color='light-blue'>
            <v-icon>add</v-icon>
            <v-icon>close</v-icon>
          </v-btn>
          <v-tooltip left>
            <span>New Receiver</span>
            <v-btn fab dark color='cyan' slot='activator' @click='addReceiver'>
              <v-icon>cloud_download</v-icon>
            </v-btn>
          </v-tooltip>
          <v-tooltip left>
            <span>New Sender</span>
            <v-btn fab dark color='pink' slot='activator' @click='addSender'>
              <v-icon>cloud_upload</v-icon>
            </v-btn>
          </v-tooltip>
        </v-speed-dial>
      </v-fab-transition>
      <v-fab-transition>
        <v-speed-dial v-model='fab' hover fixed bottom right direction='top' v-show='active=="accounts"'>
          <v-btn slot='activator' fab v-model='fab' color='purple xxxlighten-2'>
            <v-icon>add</v-icon>
          </v-btn>
          <v-tooltip left>
            <span>Register New Account</span>
            <v-btn fab dark color='pink' slot='activator' @click=''>
              <v-icon>person_add</v-icon>
            </v-btn>
          </v-tooltip>
          <v-tooltip left>
            <span>Login to old account</span>
            <v-btn fab dark color='blue' slot='activator' @click=''>
              <v-icon>person</v-icon>
            </v-btn>
          </v-tooltip>
        </v-speed-dial>
      </v-fab-transition>
    </v-app>
  </div>
</template>
<script>
import AccountsManager from './components/AccountsManager.vue'
import ClientManager from './components/ClientManager.vue'
import { EventBus } from './event-bus'

export default {
  name: 'app',
  components: {
    AccountsManager,
    ClientManager
  },
  data( ) {
    return {
      fab: {},
      active: null
    }
  },
  methods: {
    showDev( ) {
      Interop.showDev( )
    },
    addReceiver( ) {
      EventBus.$emit( 'show-add-receiver-dialog' )
    },
    addSender( ) {
      EventBus.$emit( 'show-add-sender-dialog' )
    },
    saveClients( ) {
      Interop.saveFileClients( )
    },
    readClients( ) {
      Interop.getFileStreams( )
    },
    purgeClients( ) {
      Interop.removeAllClients( )
        .then( res => {} )
        .catch( res => {} )
    }
  }
}
</script>
<style>
body {}

#app {}

.ellipsis {
  max-width: 100%;
  text-overflow: ellipsis;
  /* Required for text-overflow to do anything */
  white-space: nowrap;
  overflow: hidden;
}

.xs-actions {
  font-size: 16px !important;
}

.layer-list {
  padding: 0;
}

.layer {
  padding: 2px 0 2px 0;
  transition: all .3s ease;
}

.layer:last-child{

}
.layers-section {
  position: relative;
  transition: all 0.3s;
  border-top: 1px dashed grey;
}
.layers-section:last-child{
  border-bottom: 1px dashed grey;
}

.layers-section:hover:after {
  opacity: 0;
}

.layers-section:after {
  content: '\A';
  position: absolute;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
  background: rgba(0, 0, 0, 0.1);
  opacity: 1;
  transition: all 0.3s;
  pointer-events: none;
}
</style>
<template>
  <v-card class='receiver-content'>
    <!-- header - menu and title -->
    <v-layout align-center>
      <!-- speed dial menu -->
      <v-flex xs2 text-xs-center>
        <v-speed-dial v-model='fab' direction='right' left style='left:0' class='pa-0 ma-0'>
          <v-btn fab small :flat='paused' class='ma-0 light-blue elevation-0' slot='activator' v-model='fab' :loading='client.isLoading' :dark='!paused'>
            <v-icon>
              arrow_upward
            </v-icon>
            <v-icon>close</v-icon>
          </v-btn>
          <v-tooltip bottom>
            Add or remove objects from the stream.
            <v-btn fab small class='yellow darken-3 mr-1' slot='activator' @click.native='showAddRemoveDialog = true'>
              <v-icon>swap_horiz</v-icon>
            </v-btn>
          </v-tooltip>
          <v-btn fab small @click.native='togglePause' class='ma-1 black' dark>
            <v-icon>{{ paused ? "pause" : "play_arrow" }}</v-icon>
          </v-btn>
          <v-btn fab small class='red ma-1' @click.native='confirmDelete=true'>
            <v-icon>delete</v-icon>
          </v-btn>
        </v-speed-dial>
      </v-flex>
      <!-- title -->
      <v-flex xs10>
        <v-card-title primary-title class='pb-0 pl-1 pt-3' :class='{ faded: fab }' style='transition: all .3s ease;' @mouseenter='showEditTitle=true' @mouseleave='showEditTitle=false'>
          <span class='headline mb-1 breaklines'>
            {{ client.stream.name }}
          <v-fade-transition>
            <v-tooltip bottom>
              Edit name
              <v-btn icon @click.native='toggleTitleEdit' small v-show='showEditTitle' class='pa-0 ma-0' slot='activator'>
                <v-icon class='xs-actions'>edit</v-icon>
              </v-btn>
            </v-tooltip>
          </v-fade-transition>
          </span>
          <div class='caption' style='display: block; width:100%'> <span class='grey--text text--darkenx'><code class='grey darken-2 white--text' style='user-select: all; cursor: pointer;'>{{ client.stream.streamId }}</code> <span v-show='client.progressMessage==null'>{{paused ? "(paused)" : ""}} updated:
              <timeago :auto-update='10' :since='client.lastUpdate'></timeago> </span>{{client.progressMessage}} </span>
          </div>
        </v-card-title>
      </v-flex>
    </v-layout>
    <!-- progress bar -->
    <!-- <v-progress-linear height='1' :indeterminate='true' v-if='client.isLoading'></v-progress-linear> -->
    <!-- expired alert -->
    <v-alert color='info' v-model='client.expired' class='pb-0 pt-0 mt-3'>
      <v-layout>
        <v-flex class='text-xs-center'>Stream is outdated.
          <v-tooltip left>
            Force refresh.
            <v-btn dark small fab flat @click.native='refreshStream' slot='activator' class='ma-0 '>
              <v-icon>refresh</v-icon>
            </v-btn>
          </v-tooltip>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- error alert -->
    <v-alert color='error' v-model='hasError' class='mt-4'>
      <v-layout align-center>
        <v-flex>Error: {{ client.error }}
          <v-tooltip left>
            Dismiss.
            <v-btn dark small fab flat @click.native='client.error=null' slot='activator' class='ma-0'>
              <v-icon>close</v-icon>
            </v-btn>
          </v-tooltip>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- standard actions -->
    <v-card-actions v-show='true' class='pl-2'>
      <v-spacer></v-spacer>
      <v-btn icon @click.native='toggleLayers' small>
        <v-icon class='xs-actions'>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <!-- <v-btn icon @click.native='toggleLog' small>
          <v-icon class='xs-actions'>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
        </v-btn> -->
<!--       <v-btn icon @click.native='toggleChildren' small>
        <v-icon class='xs-actions'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
      </v-btn> -->
      <extra-view-menu :streamId='client.stream.streamId' :restApi='client.BaseUrl'></extra-view-menu>
    </v-card-actions>
    <!-- layers -->
    <v-slide-y-transition>
      <div v-show='showLayers' class='pa-0'>
        <sender-layers :layers='client.stream.layers' :objects='client.stream.objects' :clientId='client.ClientId'></sender-layers>
      </div>
    </v-slide-y-transition>
    <!-- log -->
    <v-slide-y-transition>
      <v-card-text v-show='showLog' class='pa-0'>
        <!-- <blockquote class='section-title'>Log</blockquote> -->
        <div class='caption pa-2'>Client id: <code>{{client.ClientId}}</code></div>
        <div class='log pa-2'>
          <template v-for='log in client.log'>
            <div class='caption' mb-5>
              <v-divider></v-divider>
              {{ log.timestamp }}: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
      </v-card-text>
    </v-slide-y-transition>
    <!-- history -->
    <v-slide-y-transition>
      <v-card-text v-show='showChildren' xxxclass='grey darken-4'>
        History: todo
      </v-card-text>
    </v-slide-y-transition>
    <!-- add objects dialog -->
    <v-dialog fullscreen v-model='showAddRemoveDialog'>
      <v-card>
        <v-toolbar style="flex: 0 0 auto;" dark>
          <v-btn icon @click.native="showAddRemoveDialog = false" dark>
            <v-icon>close</v-icon>
          </v-btn>
          <v-toolbar-title>Add or Remove to {{client.stream.name}}</v-toolbar-title>
        </v-toolbar>
        <v-card-text>
          <div class='headline'>Based on your selection, there are <strong>{{selectionObjectCount}} </strong> objects on {{objectSelection.length}} layers.</div>
          <div class='body-1'>You can still edit your selection.</div>
          <div syle='width:100%' class='pa-3'>
            <template v-for='sel in objectSelection'>
              <v-chip xxxsmall class='eliptic caption' style='text-align: left; max-width: 40%;' slot='activator'>
                <v-avatar :style='{ backgroundColor: sel.color }'>{{sel.objectCount}}</v-avatar>
                {{sel.layerName}}
              </v-chip>
              </v-tooltip>
            </template>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-btn flat @click.native='showAddRemoveDialog=false'>cancel</v-btn>
          <v-spacer></v-spacer>
          <v-btn block color='light-blue' @click.native='addObjectsToStream'>Add</v-btn>
          <v-btn color='red' @click.native='removeObjectsFromStream'>Remove</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <!-- confirm delete dialog -->
    <v-dialog v-model='confirmDelete'>
      <v-card>
        <v-card-title class='headline'>Are you sure you want to delete this sender?</v-card-title>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native='confirmDelete=false'>Cancel</v-btn>
          <v-btn color='red' class='' @click.native='removeClient'>Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <!-- change name -->
    <v-dialog v-model='editTitle'>
      <v-card>
        <!-- <v-card-title class='headline'>Edit stream title</v-card-title> -->
        <v-card-text>
          <v-text-field label='New stream name' v-model='newStreamName' v-validate="'required|min:3|max:42'" :error-messages="errors.collect('name')" data-vv-name='name'></v-text-field>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native='editTitle=false'>Cancel</v-btn>
          <v-btn color='light-blue' class='' @click.native='saveStreamName' :loading='streamNameSaving'>Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>
<script>
import API from '../store/apicaller.js'

import SenderLayers from './SenderLayers.vue'
import ExtraViewMenu from './ExtraViewMenu.vue'

export default {
  name: 'Sender',
  props: {
    client: Object
  },
  components: {
    SenderLayers,
    ExtraViewMenu
  },
  watch: {
    'client.error' ( value ) {
      console.log( "ERRRR", value )
    }
  },
  computed: {
    objectSelection( ) { return this.$store.getters.selection },
    selectionObjectCount( ) {
      let sum = 0
      this.objectSelection.forEach( l => sum += l.objectCount )
      return sum
    },
    layerInfo( ) { return this.$store.getters.layerInfo },
    hasError( ) { return this.client.error != '' && this.client.error != null }
  },
  data( ) {
    return {
      fab: false,
      confirmDelete: false,
      showLayers: false,
      showLog: false,
      showChildren: false,
      showMenu: false,
      showAddRemoveDialog: false,
      paused: false,
      showEditTitle: true,
      editTitle: false,
      newStreamName: null,
      streamNameSaving: false
    }
  },
  methods: {
    addObjectsToStream( ) {
      let guids = this.objectSelection.reduce( ( acc, obj ) => [ ...obj.objectGuids, ...acc ], [ ] )
      Interop.addRemoveObjects( this.client.ClientId, JSON.stringify( guids ), false )
      this.showAddRemoveDialog = false
    },
    removeObjectsFromStream( ) {
      let guids = this.objectSelection.reduce( ( acc, obj ) => [ ...obj.objectGuids, ...acc ], [ ] )
      Interop.addRemoveObjects( this.client.ClientId, JSON.stringify( guids ), true )
      this.showAddRemoveDialog = false
    },
    togglePause( ) {
      this.paused = !this.paused
      Interop.setClientPause( this.client.ClientId, this.paused )
    },
    toggleLog( ) {
      if ( this.showLog ) return this.showLog = false
      this.showLog = true
      this.showLayers = false
      this.showChildren = false
    },
    toggleLayers( ) {
      if ( this.showLayers ) return this.showLayers = false
      this.showLayers = true
      this.showLog = false
      this.showChildren = false
    },
    toggleChildren( ) {
      if ( this.showChildren ) return this.showChildren = false
      this.showLayers = false
      this.showLog = false
      this.showChildren = true
    },
    removeClient( ) {
      this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
    },
    refreshStream( ) {
      this.client.expired = false
      this.killError( )
      Interop.forceSend( this.client.ClientId )
    },
    killError( ) {
      this.client.error = null
    },
    toggleTitleEdit( ) {
      this.editTitle = !this.editTitle
      this.newStreamName = this.client.stream.name
    },
    saveStreamName( ) {
      this.$validator.validateAll( ).then( result => {
        if ( !result ) return
        this.streamNameSaving = true
        this.client.stream.name = this.newStreamName
        API.updateStreamName( this.client )
          .then( res => {
            this.streamNameSaving = false
            this.editTitle = false // hide dialog
            Interop.setName( this.client.ClientId, this.client.stream.name )
          } )
      } )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.breaklines {
  word-break: break-all;
  hyphens: auto;
}

.faded {
  opacity: 0.2
}

.stream-menu {
  position: absolute;
}

.fat-one {
  /*width:100%;*/
}

.make-me-small {
  font-size: 15px !important;
}
</style>